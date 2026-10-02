import assert from 'node:assert/strict';
import test from 'node:test';
import { readFileSync } from 'node:fs';
import { hasPermission } from '../lib/permissions';
import { formatMoney, formatCompactMoney, humanizeStatus, statusTone } from '../lib/formatters';
import { getCustomerRouteSlug, resolveCustomerRouteSlug, withCustomerBase } from '../lib/tenant-route';

test('compact money keeps currency visible and avoids raw ledger integers', () => {
  assert.equal(formatCompactMoney(989339800, 'AED'), 'AED 989.3M');
  assert.equal(formatCompactMoney(42400, 'AED'), 'AED 42.4K');
  assert.equal(formatCompactMoney(null), 'Not available');
});

test('permission matching accepts permission keys and readable names', () => {
  const permissions = [
    { key: 'RECEIVING_INVOICES', name: 'Invoice receiving' },
    { key: 'ADMIN_USERS', name: 'Users' },
  ];

  assert.equal(hasPermission(permissions, 'receiving-invoices'), true);
  assert.equal(hasPermission(permissions, 'user'), true);
  assert.equal(hasPermission(permissions, 'inventory'), false);
});

test('permission matching denies missing or unresolved context', () => {
  assert.equal(hasPermission(undefined, 'admin'), false);
  assert.equal(hasPermission([], ['admin', 'organization']), false);
});

test('status labels stay human-readable while preserving semantic tones', () => {
  assert.equal(humanizeStatus('PARTIALLY_RECEIVED'), 'Partially Received');
  assert.equal(statusTone('GRN_POSTED'), 'good');
  assert.equal(statusTone('REVIEW_REQUIRED'), 'warn');
  assert.equal(statusTone('OCR_FAILED'), 'bad');
});

test('money formatting uses the Cloud default currency when present', () => {
  assert.match(formatMoney(1250.5), /1,250\.50/);
  assert.equal(formatMoney(null), '—');
});

test('customer route slugs use canonical five without treating app routes as tenants', () => {
  assert.equal(getCustomerRouteSlug('/five/dashboard'), 'five');
  assert.equal(getCustomerRouteSlug('/five-test/platform-launch'), 'five');
  assert.equal(getCustomerRouteSlug('/platform-launch'), null);
  assert.equal(getCustomerRouteSlug('/organizations'), null);
  assert.equal(getCustomerRouteSlug('/dashboard'), null);
  assert.equal(getCustomerRouteSlug('/login'), null);
  assert.equal(resolveCustomerRouteSlug('/five/login'), 'five');
  assert.equal(withCustomerBase('/login', 'five'), '/five/login');
});

test('Cloud login keeps the SILA ME Cloud sign-in screen on /five/login', () => {
  const page = readFileSync(new URL('../pages/login.tsx', import.meta.url), 'utf8');
  const boot = readFileSync(new URL('../main.tsx', import.meta.url), 'utf8');
  assert.ok(page.includes('<h2>SILA ME Cloud</h2>'));
  assert.ok(page.includes('Secure enterprise access'));
  assert.ok(page.includes('Menu Engineering'));
  assert.ok(!page.includes("{tenant?.customerName || 'Customer login'}"));
  assert.ok(boot.includes('setRouteSlug(resolveCustomerRouteSlug())'));
});

test('Cloud shell does not keep Property or Store as global application context', () => {
  const layout = readFileSync(new URL('../components/sila-layout.tsx', import.meta.url), 'utf8');
  assert.ok(!layout.includes('select-property'));
  assert.ok(!layout.includes('select-operating-unit'));
  assert.ok(!layout.includes('sila-context-group'));
  assert.ok(layout.includes('banner-customer-context'));
});
