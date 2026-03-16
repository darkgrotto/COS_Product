import { api } from './client';
import { GradingAgency } from '../types/gradingAgency';

export interface GradingAgencyCreateRequest {
  code: string;
  fullName: string;
  validationUrlTemplate: string;
  supportsDirectLookup: boolean;
}

export interface GradingAgencyPatchRequest {
  fullName?: string;
  validationUrlTemplate?: string;
  supportsDirectLookup?: boolean;
}

export interface GradingAgencyDeleteRequest {
  replacementCode?: string;
}

export interface GradingAgencyDeleteConflict {
  requiresReplacement: boolean;
  recordCount: number;
}

export const gradingAgenciesApi = {
  getAll: (): Promise<GradingAgency[]> =>
    api.get<GradingAgency[]>('/api/grading-agencies'),

  create: (request: GradingAgencyCreateRequest): Promise<GradingAgency> =>
    api.post<GradingAgency>('/api/grading-agencies', request),

  patch: (code: string, request: GradingAgencyPatchRequest): Promise<GradingAgency> =>
    api.patch<GradingAgency>(`/api/grading-agencies/${code}`, request),

  delete: (code: string, request?: GradingAgencyDeleteRequest): Promise<void> =>
    api.delete<void>(`/api/grading-agencies/${code}`, request),
};
