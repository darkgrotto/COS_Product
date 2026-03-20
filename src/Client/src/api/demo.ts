export interface DemoStatus {
  isDemo: true;
  expiresAt: string | null;
  secondsRemaining: number;
  visitorId: string;
  demoSets: string[];
}

export const demoApi = {
  async getStatus(): Promise<DemoStatus | null> {
    const res = await fetch('/api/demo/status');
    if (res.status === 404) return null;
    if (!res.ok) return null;
    return res.json() as Promise<DemoStatus>;
  },
};
