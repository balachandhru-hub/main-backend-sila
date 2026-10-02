export type ItoStatus =
  | 'DRAFT'
  | 'SUBMITTED'
  | 'SOURCE_APPROVAL_PENDING'
  | 'DESTINATION_APPROVAL_PENDING'
  | 'APPROVED'
  | 'REJECTED'
  | 'READY_FOR_DISPATCH'
  | 'PARTIALLY_DISPATCHED'
  | 'IN_TRANSIT'
  | 'PARTIALLY_RECEIVED'
  | 'RECEIVED'
  | 'COMPLETED'
  | 'CANCELLED'
  | 'FAILED';

export type ItoApprovalState = 'PENDING' | 'APPROVED' | 'REJECTED' | 'SENT_BACK';

export type ItoPriority = 'NORMAL' | 'HIGH' | 'URGENT';

export type ItoLineDraft = {
  key: string;
  materialId: string;
  materialCode: string;
  description: string;
  uom: string;
  availableQty: number | null;
  requestedQty: string;
  category?: string | null;
};

export type ItoLocationRef = {
  organizationId: string;
  organizationName: string;
  unitId: string;
  unitName: string;
  unitCode: string;
};

export type ItoDraft = {
  source: ItoLocationRef | null;
  destination: ItoLocationRef | null;
  lines: ItoLineDraft[];
  reason: string;
  priority: ItoPriority;
  notes: string;
};

export type ItoTimelineEvent = {
  id: string;
  action: string;
  actor?: string | null;
  at?: string | null;
  comment?: string | null;
  completed: boolean;
};

export const ITO_STATUS_LABELS: Record<ItoStatus, string> = {
  DRAFT: 'Draft',
  SUBMITTED: 'Submitted',
  SOURCE_APPROVAL_PENDING: 'Source approval pending',
  DESTINATION_APPROVAL_PENDING: 'Destination approval pending',
  APPROVED: 'Approved',
  REJECTED: 'Rejected',
  READY_FOR_DISPATCH: 'Ready for dispatch',
  PARTIALLY_DISPATCHED: 'Partially dispatched',
  IN_TRANSIT: 'In transit',
  PARTIALLY_RECEIVED: 'Partially received',
  RECEIVED: 'Received',
  COMPLETED: 'Completed',
  CANCELLED: 'Cancelled',
  FAILED: 'Failed',
};

export function validateSourceDestination(sourceUnitId?: string | null, destinationUnitId?: string | null) {
  if (!sourceUnitId || !destinationUnitId) return null;
  if (sourceUnitId === destinationUnitId) return 'SOURCE_DESTINATION_SAME';
  return null;
}

export function validateRequestedQty(requested: string, available: number | null) {
  const qty = Number(requested);
  if (!requested.trim() || Number.isNaN(qty) || qty <= 0) return 'Enter a valid requested quantity.';
  if (available != null && qty > available) return 'Requested quantity cannot exceed available stock at source.';
  return null;
}

export function bothPartiesApproved(source: ItoApprovalState, destination: ItoApprovalState) {
  return source === 'APPROVED' && destination === 'APPROVED';
}

export function buildItoTimeline(params: {
  createdAt?: string | null;
  submittedAt?: string | null;
  sourceApprovedAt?: string | null;
  destinationApprovedAt?: string | null;
  dispatchedAt?: string | null;
  receivedAt?: string | null;
  completedAt?: string | null;
  createdBy?: string | null;
  sourceApprover?: string | null;
  destinationApprover?: string | null;
  dispatcher?: string | null;
  receiver?: string | null;
}): ItoTimelineEvent[] {
  return [
    { id: 'created', action: 'ITO Created', actor: params.createdBy, at: params.createdAt, completed: Boolean(params.createdAt) },
    { id: 'submitted', action: 'Submitted', actor: params.createdBy, at: params.submittedAt, completed: Boolean(params.submittedAt) },
    { id: 'source', action: 'Source Approved', actor: params.sourceApprover, at: params.sourceApprovedAt, completed: Boolean(params.sourceApprovedAt) },
    { id: 'destination', action: 'Destination Approved', actor: params.destinationApprover, at: params.destinationApprovedAt, completed: Boolean(params.destinationApprovedAt) },
    { id: 'dispatched', action: 'Dispatched', actor: params.dispatcher, at: params.dispatchedAt, completed: Boolean(params.dispatchedAt) },
    { id: 'received', action: 'Received', actor: params.receiver, at: params.receivedAt, completed: Boolean(params.receivedAt) },
    { id: 'completed', action: 'Completed', at: params.completedAt, completed: Boolean(params.completedAt) },
  ];
}

export function createEmptyItoDraft(): ItoDraft {
  return {
    source: null,
    destination: null,
    lines: [],
    reason: '',
    priority: 'NORMAL',
    notes: '',
  };
}

export function formatLocation(location: ItoLocationRef | null | undefined) {
  if (!location) return '—';
  return `${location.organizationName} — ${location.unitName}`;
}
