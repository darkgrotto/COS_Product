import { api } from './client';

export interface UpdateStatus {
  currentContentVersion: string | null;
  pendingSchemaUpdate: {
    id: number;
    schemaVersion: string;
    description: string;
    detectedAt: string;
    isApproved: boolean;
    approvedAt: string | null;
  } | null;
  latestApplicationVersion: string | null;
  applicationUpdatePending: boolean;
}

export interface AdminNotification {
  id: number;
  message: string;
  category: string;
  isRead: boolean;
  createdAt: string;
}

export const updatesApi = {
  getStatus: (): Promise<UpdateStatus> =>
    api.get<UpdateStatus>('/api/updates/status'),

  getNotifications: (): Promise<AdminNotification[]> =>
    api.get<AdminNotification[]>('/api/updates/notifications'),

  triggerCheck: (): Promise<void> =>
    api.post<void>('/api/updates/check'),

  approveSchemaUpdate: (id: number): Promise<void> =>
    api.post<void>(`/api/updates/schema/${id}/approve`),

  markNotificationRead: (id: number): Promise<void> =>
    api.post<void>(`/api/updates/notifications/${id}/read`),
};
