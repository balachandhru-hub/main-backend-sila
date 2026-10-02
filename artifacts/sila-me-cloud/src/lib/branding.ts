export function isSuperAdmin(roles?: Array<{ key: string }> | null) {
  return Boolean(roles?.some((role) => role.key === 'SUPER_ADMIN'));
}

export function organizationLogoPath(organizationId: string, hasLogo: boolean) {
  return hasLogo ? `/api/v1/access/organizations/${organizationId}/logo` : null;
}

export function resolveBrandSource(customerLogo: string | null | undefined, fallback: string) {
  return customerLogo || fallback;
}
