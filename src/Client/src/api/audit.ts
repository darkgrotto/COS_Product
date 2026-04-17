export interface AuditLogEntry {
  id: string;
  timestamp: string;
  actor: string;
  actorDisplayName: string;
  actionType: string;
  target: string | null;
  result: string;
  ipAddress: string | null;
  sessionId: string | null;
}

export const auditApi = {
  getLogs: async (limit = 100, actionType?: string): Promise<AuditLogEntry[]> => {
    const params = new URLSearchParams({ limit: String(limit) });
    if (actionType) params.set('actionType', actionType);
    const res = await fetch(`/api/audit/logs?${params}`);
    if (!res.ok) throw new Error('Failed to fetch audit logs');
    return res.json();
  },

  getActionTypes: async (): Promise<string[]> => {
    const res = await fetch('/api/audit/logs/action-types');
    if (!res.ok) throw new Error('Failed to fetch action types');
    return res.json();
  },
};
