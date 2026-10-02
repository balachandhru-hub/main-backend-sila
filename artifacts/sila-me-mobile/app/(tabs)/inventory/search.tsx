import React, { useState } from 'react';
import { SilaEmptyState, SilaNotConnected, SilaScreen, SilaSearchBar } from '@/components/sila/ui';
import { capabilityMessage } from '@/services/ops/capabilities';
import { inventoryService } from '@/services/ops';

export default function StockLookupScreen() {
  const [query, setQuery] = useState('');
  const [message, setMessage] = useState<string | null>(null);

  const search = async (value: string) => {
    setQuery(value);
    if (value.trim().length < 2) {
      setMessage(null);
      return;
    }
    const result = await inventoryService.search(value);
    if (result.status === 'NOT_IMPLEMENTED') setMessage(result.message);
  };

  return (
    <SilaScreen title="Stock lookup" eyebrow="INVENTORY" fallback="/inventory">
      <SilaSearchBar value={query} onChangeText={(value) => void search(value)} placeholder="Material ID, description, or barcode" />
      <SilaNotConnected message={message ?? capabilityMessage('inventoryLookup')} />
      <SilaEmptyState title="No stock results." description="Connected inventory lookup will appear here when the API is available." />
    </SilaScreen>
  );
}
