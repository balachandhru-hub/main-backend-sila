import { Redirect, useLocalSearchParams } from 'expo-router';

export default function PurchaseOrderRedirect() {
  const { id } = useLocalSearchParams<{ id: string }>();
  return <Redirect href={{ pathname: '/receive/po/[id]', params: { id } }} />;
}
