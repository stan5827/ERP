# UAT and production deployment

The CI workflow publishes backend and frontend images tagged `sha-<full-commit-sha>` and scans both images for high and critical vulnerabilities before UAT deployment. A push to `main` or `master` deploys that immutable tag to UAT first. Production runs only after UAT is healthy and the `production` GitHub Environment grants approval.

## GitHub setup

Create `uat` and `production` environments in repository settings. Configure required reviewers and prevent self-review in `production`; naming an environment in workflow YAML alone does not enforce approval.

Add these environment secrets to each environment (use separate host credentials and paths):

- `DEPLOY_HOST`: Linux host DNS name.
- `DEPLOY_PORT`: optional SSH port; defaults to `22`.
- `DEPLOY_USER`: SSH deployment account.
- `DEPLOY_PATH`: absolute application directory, without spaces.
- `DEPLOY_SSH_PRIVATE_KEY`: private key authorized for that account.
- `DEPLOY_SSH_KNOWN_HOSTS`: host key entry verified out of band; do not generate trust with `ssh-keyscan` during deployment.

The host must have Docker Compose v2, registry pull access for the GHCR images, and the existing monitoring configuration files referenced by `docker-compose.yml`. Keep production configuration and secrets in the host-side `.env`; do not add them to the repository.

## Host preparation

Before enabling either deployment job, prepare each environment's directory with:

- `.env` containing the required settings (including database, JWT, and Grafana credentials).
- `.image-tag.env` containing `IMAGE_TAG=sha-<full-commit-sha>` for the currently running, verified version. This is required as the automatic rollback target.
- `deployment/` and its existing monitoring configuration directories.

Verify that the tag in `.image-tag.env` can be pulled from GHCR before enabling deployment. The workflow uploads the Compose file, pulls only the backend and frontend images, waits for Compose health checks, and restores the previous tag and services if deployment fails.

Do not approve production deployment until the UAT data/schema compatibility checks, database backup, and maintenance-window checks have passed. If automatic rollback cannot restore healthy services, stop further deployments and recover using the verified prior image and database backup.
