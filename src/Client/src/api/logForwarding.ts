import { api } from './client';

export const LOG_LEVELS = ['Trace', 'Debug', 'Information', 'Warning', 'Error', 'Critical'] as const;
export type LogLevel = typeof LOG_LEVELS[number];

export interface LogForwardingConfig {
  enabled: boolean;
  destinationUrl: string | null;
  authHeaderSet: boolean;
  minLevel: LogLevel;
}

export interface SaveLogForwardingConfig {
  enabled: boolean;
  destinationUrl: string | null;
  authHeader: string | null; // null = keep existing; '' = clear
  minLevel: LogLevel;
}

export const logForwardingApi = {
  getConfig: (): Promise<LogForwardingConfig> =>
    api.get<LogForwardingConfig>('/api/log-forwarding/config'),

  saveConfig: (config: SaveLogForwardingConfig): Promise<void> =>
    api.put<void>('/api/log-forwarding/config', config),

  sendTest: (): Promise<{ message: string }> =>
    api.post<{ message: string }>('/api/log-forwarding/test'),
};
