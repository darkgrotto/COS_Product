export interface GradingAgency {
  code: string;
  fullName: string;
  validationUrlTemplate: string;
  supportsDirectLookup: boolean;
  source: 'Canonical' | 'Local';
  active: boolean;
}
