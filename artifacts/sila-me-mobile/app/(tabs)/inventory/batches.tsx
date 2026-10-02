import React, { useState } from 'react';
import { View } from 'react-native';
import { SilaBottomAction, SilaEmptyState, SilaNotConnected, SilaScreen } from '@/components/sila/ui';
import { capabilityMessage } from '@/services/ops/capabilities';

const filters = ['Expiring Today', '7 Days', '30 Days', '60 Days', 'Expired'] as const;

export default function BatchExpiryScreen() {
  const [filter, setFilter] = useState<(typeof filters)[number]>('7 Days');
  return (
    <SilaScreen title="Batch & expiry" eyebrow="INVENTORY" fallback="/inventory">
      <View style={{ gap: 8, marginBottom: 12 }}>
        {filters.map((item) => (
          <SilaBottomAction key={item} label={item} secondary={filter !== item} onPress={() => setFilter(item)} />
        ))}
      </View>
      <SilaNotConnected message={capabilityMessage('batchExpiry')} />
      <SilaEmptyState title="No batch or expiry records." description={`Filter: ${filter}`} />
    </SilaScreen>
  );
}
