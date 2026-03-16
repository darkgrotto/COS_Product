import { useEffect, useState } from 'react';

interface LicenseInfo {
  name: string;
  fullName: string;
  url: string;
}

interface AboutData {
  currentVersion: string;
  latestReleasedVersion: string;
  updatePending: boolean;
  lastContentUpdate: string | null;
  instanceName: string;
  license: LicenseInfo;
}

export function AboutView() {
  const [data, setData] = useState<AboutData | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetch('/api/about')
      .then((res) => {
        if (!res.ok) throw new Error(`Request failed: ${res.status}`);
        return res.json() as Promise<AboutData>;
      })
      .then(setData)
      .catch((err: unknown) => {
        setError(err instanceof Error ? err.message : 'Failed to load about information');
      });
  }, []);

  if (error) {
    return <p>Error loading about information: {error}</p>;
  }

  if (!data) {
    return <p>Loading...</p>;
  }

  return (
    <div>
      <h1>About {data.instanceName}</h1>

      <section>
        <h2>Application Version</h2>
        <table>
          <tbody>
            <tr>
              <th>Current version</th>
              <td>{data.currentVersion}</td>
            </tr>
            <tr>
              <th>Latest released version</th>
              <td>{data.latestReleasedVersion}</td>
            </tr>
            <tr>
              <th>Update pending</th>
              <td>{data.updatePending ? 'Yes' : 'No'}</td>
            </tr>
            <tr>
              <th>Last content update</th>
              <td>{data.lastContentUpdate ?? 'No content updates applied'}</td>
            </tr>
          </tbody>
        </table>
      </section>

      <section>
        <h2>License</h2>
        <p>
          CountOrSell is licensed under{' '}
          <a href={data.license.url} target="_blank" rel="noopener noreferrer">
            {data.license.name}
          </a>{' '}
          ({data.license.fullName}).
        </p>
        <p>
          You may use, share, and adapt it for non-commercial purposes provided
          you give attribution and distribute any adaptations under the same
          license.
        </p>
      </section>
    </div>
  );
}
