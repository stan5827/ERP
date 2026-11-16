#!/usr/bin/env bash
set -Eeuo pipefail

deploy_dir="${1:?deployment directory is required}"
image_tag="${2:?image tag is required}"
compose_file="$deploy_dir/deployment/docker-compose.yml"
tag_file="$deploy_dir/.image-tag.env"

if [[ ! "$deploy_dir" =~ ^/[A-Za-z0-9._/-]+$ || "$deploy_dir" == *"/../"* || "$deploy_dir" == */.. ]]; then
    echo "DEPLOY_PATH must be a safe absolute path." >&2
    exit 2
fi

if [[ ! "$image_tag" =~ ^sha-[0-9a-f]{40}$ ]]; then
    echo "Image tag must be a full immutable commit SHA tag." >&2
    exit 2
fi

if [[ ! -f "$deploy_dir/.env" || ! -f "$tag_file" || ! -f "$compose_file" ]]; then
    echo "Production .env, .image-tag.env, and deployment/docker-compose.yml must exist." >&2
    exit 2
fi

previous_tag="$(sed -n 's/^IMAGE_TAG=//p' "$tag_file")"
if [[ ! "$previous_tag" =~ ^sha-[0-9a-f]{40}$ ]]; then
    echo ".image-tag.env must contain one previously validated full SHA image tag." >&2
    exit 2
fi

compose() {
    docker compose \
        --env-file "$deploy_dir/.env" \
        --env-file "$tag_file" \
        --file "$compose_file" \
        "$@"
}

write_image_tag() {
    local value="$1"
    local temporary_file="${tag_file}.tmp.$$"
    printf 'IMAGE_TAG=%s\n' "$value" > "$temporary_file"
    mv "$temporary_file" "$tag_file"
}

rollback_on_error() {
    local exit_code=$?
    trap - ERR
    echo "Deployment failed; restoring image tag $previous_tag." >&2
    write_image_tag "$previous_tag"
    if ! compose up -d --wait --wait-timeout 180 backend frontend; then
        echo "Automatic rollback did not become healthy; operator action is required." >&2
    fi
    exit "$exit_code"
}

cd "$deploy_dir"
docker compose version >/dev/null
compose config --quiet
trap rollback_on_error ERR
write_image_tag "$image_tag"
compose config --quiet
compose pull backend frontend
compose up -d --wait --wait-timeout 180 backend frontend
compose ps backend frontend
trap - ERR
