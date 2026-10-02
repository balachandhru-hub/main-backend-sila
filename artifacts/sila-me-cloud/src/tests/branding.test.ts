import assert from 'node:assert/strict';
import test from 'node:test';
import { isSuperAdmin, organizationLogoPath, resolveBrandSource } from '../lib/branding';

test('only SUPER_ADMIN can manage customer logo uploads', () => {
  assert.equal(isSuperAdmin([{ key: 'SUPER_ADMIN' }]), true);
  assert.equal(isSuperAdmin([{ key: 'CUSTOMER_ADMIN' }]), false);
  assert.equal(isSuperAdmin([{ key: 'PLATFORM_ADMIN' }, { key: 'VIEWER' }]), false);
  assert.equal(isSuperAdmin([]), false);
  assert.equal(isSuperAdmin(null), false);
});

test('logo URL is emitted only when a customer logo exists', () => {
  assert.equal(
    organizationLogoPath('11111111-1111-1111-1111-111111111111', true),
    '/api/v1/access/organizations/11111111-1111-1111-1111-111111111111/logo',
  );
  assert.equal(organizationLogoPath('11111111-1111-1111-1111-111111111111', false), null);
});

test('branding display falls back to SILA when customer logo is absent', () => {
  assert.equal(resolveBrandSource('blob:customer', 'sila.png'), 'blob:customer');
  assert.equal(resolveBrandSource(null, 'sila.png'), 'sila.png');
  assert.equal(resolveBrandSource(undefined, 'sila.png'), 'sila.png');
  assert.equal(resolveBrandSource('', 'sila.png'), 'sila.png');
});
