import { api } from './client';

export interface BackupRecord {
  id: string;
  label: string;
  backupType: string;
  schemaVersion: string;
  createdAt: string;
  fileSizeBytes: number;
  isAvailable: boolean;
}

export interface BackupStatusDestination {
  id: string;
  type: string;
  label: string;
  isActive: boolean;
}

export interface BackupStatus {
  lastScheduledBackup: {
    id: string;
    label: string;
    createdAt: string;
    schemaVersion: string;
  } | null;
  lastPreUpdateBackup: {
    id: string;
    label: string;
    createdAt: string;
    schemaVersion: string;
  } | null;
  nextScheduledBackup: string;
  destinations: BackupStatusDestination[];
}

export interface BackupHistoryPage {
  total: number;
  page: number;
  pageSize: number;
  records: BackupRecord[];
}

export interface AddDestinationRequest {
  destinationType: string;
  label: string;
  configurationJson: string;
}

export const backupApi = {
  getStatus: (): Promise<BackupStatus> =>
    api.get<BackupStatus>('/api/backup/status'),

  getHistory: (page = 1, pageSize = 20): Promise<BackupHistoryPage> =>
    api.get<BackupHistoryPage>(`/api/backup/history?page=${page}&pageSize=${pageSize}`),

  trigger: (): Promise<{ id: string; label: string; createdAt: string }> =>
    api.post('/api/backup/trigger'),

  getDestinations: (): Promise<BackupStatusDestination[]> =>
    api.get<BackupStatusDestination[]>('/api/backup/destinations'),

  addDestination: (request: AddDestinationRequest): Promise<BackupStatusDestination> =>
    api.post<BackupStatusDestination>('/api/backup/destinations', request),

  removeDestination: (id: string): Promise<void> =>
    api.delete<void>(`/api/backup/destinations/${id}`),

  testDestination: (id: string): Promise<{ success: boolean }> =>
    api.post<{ success: boolean }>(`/api/backup/destinations/${id}/test`),

  downloadUrl: (id: string): string =>
    `/api/backup/${id}/download`,

  restore: (file: File): Promise<{ restoredSchemaVersion: string }> => {
    const formData = new FormData();
    formData.append('file', file);
    return fetch('/api/restore', {
      method: 'POST',
      credentials: 'include',
      body: formData,
    }).then(async (r) => {
      if (!r.ok) {
        const text = await r.text().catch(() => '');
        throw new Error(`HTTP ${r.status}: ${text}`);
      }
      return r.json() as Promise<{ restoredSchemaVersion: string }>;
    });
  },

  restoreFromRecord: (backupId: string): Promise<{ restoredSchemaVersion: string }> =>
    api.post(`/api/restore/${backupId}`),
};
