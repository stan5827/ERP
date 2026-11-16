export interface SearchableSystemUser {
  username: string;
  email?: string;
}

const normalizeRoleName = (roleName: string) =>
  roleName.trim().toLocaleLowerCase().replace(/[\s_-]+/g, '');

export function getRoleDisplayName(roleName: string, displayNames: Record<string, string>): string {
  const normalizedRoleName = normalizeRoleName(roleName);
  const entry = Object.entries(displayNames)
    .find(([name]) => normalizeRoleName(name) === normalizedRoleName);
  return entry?.[1] ?? roleName;
}

export function matchesSystemUserSearch(
  user: SearchableSystemUser,
  employeeLabel: string | undefined,
  searchTerm: string,
): boolean {
  const query = searchTerm.trim().toLocaleLowerCase();
  if (!query)
    return true;

  return [user.username, user.email, employeeLabel]
    .some(value => value?.toLocaleLowerCase().includes(query) ?? false);
}
