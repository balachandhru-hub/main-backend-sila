export type MobileTenantContext = {
  tenantCode: string;
  tenantName: string;
  environment: 'TEST' | 'PRODUCTION' | 'DEVELOPMENT';
  routeSlug: string;
};

/**
 * Current development tenant. Later this can be replaced by a bootstrap
 * response without changing individual API screens.
 */
export const mobileTenant: MobileTenantContext = {
  tenantCode: 'five',
  tenantName: 'Five Hotels and Resorts',
  environment: 'TEST',
  routeSlug: 'five',
};
