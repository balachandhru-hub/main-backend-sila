import { Redirect } from 'expo-router';
import React from 'react';

/** Stock Transfer is replaced by Internal Transfer Order as the controlled internal movement process. */
export default function StockTransferRedirect() {
  return <Redirect href="/inventory/ito" />;
}
