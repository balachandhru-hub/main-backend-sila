export function hasPermission(permissions: Array<{ key?: string; name?: string }> | undefined, requested: string | string[]) {
  if (!permissions?.length) return false;
  const candidates = Array.isArray(requested) ? requested : [requested];
  const values = permissions.flatMap((permission) => [permission.key, permission.name].filter(Boolean).map((value) => String(value).toLowerCase().replace(/[^a-z0-9]/g, '')));
  return candidates.some((candidate) => {
    const wanted = candidate.toLowerCase().replace(/[^a-z0-9]/g, '');
    return values.includes(wanted) || values.some((value) => value.includes(wanted) || wanted.includes(value));
  });
}