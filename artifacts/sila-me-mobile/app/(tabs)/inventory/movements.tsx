import React, { useState } from 'react';
import { View } from 'react-native';
import { SilaBottomAction, SilaEmptyState, SilaNotConnected, SilaScreen, SilaSearchBar } from '@/components/sila/ui';
import { capabilityMessage } from '@/services/ops/capabilities';

const types = ['GRN RECEIPT', 'TRANSFER', 'GOODS ISSUE', 'COUNT ADJUSTMENT', 'DAMAGE', 'REJECTION', 'RETURN'] as const;

export default function MovementHistoryScreen() {
  const [query, setQuery] = useState('');
  const [type, setType] = useState<(typeof types)[number] | null>(null);
  return (
    <SilaScreen title="Movement history" eyebrow="INVENTORY" fallback="/inventory">
      <SilaSearchBar value={query} onChangeText={setQuery} placeholder="Material, document, user, location" />
      <View style={{ gap: 8, marginBottom: 12 }}>
        {types.map((item) => (
          <SilaBottomAction key={item} label={item} secondary={type !== item} onPress={() => setType(item)} />
        ))}
      </View>
      <SilaNotConnected message={capabilityMessage('movementHistory')} />
      <SilaEmptyState title="No movements to display." description="Only backend-provided movements will appear here." />
    </SilaScreen>
  );
}
