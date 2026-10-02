import React, { useState } from 'react';
import { View } from 'react-native';
import { SilaBottomAction, SilaEmptyState, SilaNotConnected, SilaScreen, SilaSearchBar } from '@/components/sila/ui';
import { capabilityMessage } from '@/services/ops/capabilities';

const categories = ['Invoices', 'Delivery Notes', 'GRN Documents', 'Stock Count Documents', 'Transfer Documents', 'Other'] as const;

export default function DocumentsScreen() {
  const [query, setQuery] = useState('');
  const [category, setCategory] = useState<(typeof categories)[number]>('Invoices');
  return (
    <SilaScreen title="Documents" eyebrow="MORE" fallback="/more">
      <SilaSearchBar value={query} onChangeText={setQuery} placeholder="Document number, supplier, PO, invoice, date" />
      <View style={{ gap: 8, marginBottom: 12 }}>
        {categories.map((item) => (
          <SilaBottomAction key={item} label={item} secondary={category !== item} onPress={() => setCategory(item)} />
        ))}
      </View>
      <SilaNotConnected message={capabilityMessage('documents')} />
      <SilaEmptyState title="No documents in this category." description={`${category} catalog will appear when connected.`} />
    </SilaScreen>
  );
}
