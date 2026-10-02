export * from "./generated/api";
export * from "./generated/api.schemas";
export { customFetch, setBaseUrl, setRouteSlug, getRouteSlug, applyTenantContext, setAuthTokenGetter, setUnauthorizedHandler } from "./custom-fetch";
export type { AuthTokenGetter, UnauthorizedHandler } from "./custom-fetch";
