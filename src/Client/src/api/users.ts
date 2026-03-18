import { api } from './client';

export interface UserPreferences {
  setCompletionRegularOnly: boolean;
  defaultPage: string | null;
}

export const usersApi = {
  getPreferences: () =>
    api.get<UserPreferences>('/api/users/me/preferences'),

  patchPreferences: (prefs: Partial<UserPreferences>) =>
    api.patch<void>('/api/users/me/preferences', prefs),

  getById: (id: string) =>
    api.get<{ id: string; username: string; displayName: string | null }>(`/api/users/${id}`),
};
