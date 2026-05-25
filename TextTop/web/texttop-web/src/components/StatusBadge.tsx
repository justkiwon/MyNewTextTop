import type { MemoStatus } from '../models/memo.ts';

interface StatusBadgeProps {
  status: MemoStatus | 'online' | 'offline';
}

const labels: Record<StatusBadgeProps['status'], string> = {
  synced: 'Synced',
  editing: 'Editing',
  saved: 'Saved',
  conflict: 'Conflict',
  offline: 'Offline',
  online: 'Online',
};

export function StatusBadge({ status }: StatusBadgeProps) {
  return <span className={`status status-${status}`}>{labels[status]}</span>;
}
