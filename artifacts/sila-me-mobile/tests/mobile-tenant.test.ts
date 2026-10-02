import assert from 'node:assert/strict';
import test from 'node:test';
import { mobileTenant } from '../config/mobile-tenant';

test('development mobile tenant is permanently bound to FIVE', () => {
  assert.equal(mobileTenant.tenantCode, 'five');
  assert.equal(mobileTenant.tenantName, 'Five Hotels and Resorts');
  assert.equal(mobileTenant.environment, 'TEST');
  assert.equal(mobileTenant.routeSlug, 'five');
  assert.equal('databaseName' in mobileTenant, false);
});
