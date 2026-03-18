import { api } from './client';

export interface InstanceSettings {
  instanceName: string;
}

export interface TcgPlayerSettings {
  configured: boolean;
  maskedKey: string | null;
}

export interface SelfEnrollmentSettings {
  enabled: boolean;
}

export interface OAuthProviderConfig {
  provider: string;
  clientId: string | null;
  secretConfigured: boolean;
}

export interface BackupSettings {
  schedule: string;
  retentionScheduled: number;
  retentionPreUpdate: number;
}

export const settingsApi = {
  getInstance: (): Promise<InstanceSettings> =>
    api.get<InstanceSettings>('/api/settings/instance'),

  updateInstance: (instanceName: string): Promise<void> =>
    api.patch<void>('/api/settings/instance', { instanceName }),

  getTcgPlayer: (): Promise<TcgPlayerSettings> =>
    api.get<TcgPlayerSettings>('/api/settings/tcgplayer'),

  setTcgPlayerKey: (apiKey: string): Promise<void> =>
    api.put<void>('/api/settings/tcgplayer', { apiKey }),

  clearTcgPlayerKey: (): Promise<void> =>
    api.delete<void>('/api/settings/tcgplayer'),

  getSelfEnrollment: (): Promise<SelfEnrollmentSettings> =>
    api.get<SelfEnrollmentSettings>('/api/settings/self-enrollment'),

  updateSelfEnrollment: (enabled: boolean): Promise<void> =>
    api.patch<void>('/api/settings/self-enrollment', { enabled }),

  getOAuth: (): Promise<OAuthProviderConfig[]> =>
    api.get<OAuthProviderConfig[]>('/api/settings/oauth'),

  updateOAuthProvider: (provider: string, clientId: string, clientSecret: string): Promise<void> =>
    api.patch<void>(`/api/settings/oauth/${provider}`, { clientId, clientSecret }),

  clearOAuthProvider: (provider: string): Promise<void> =>
    api.delete<void>(`/api/settings/oauth/${provider}`),

  getBackup: (): Promise<BackupSettings> =>
    api.get<BackupSettings>('/api/settings/backup'),

  updateBackup: (settings: Partial<BackupSettings>): Promise<void> =>
    api.patch<void>('/api/settings/backup', settings),
};
