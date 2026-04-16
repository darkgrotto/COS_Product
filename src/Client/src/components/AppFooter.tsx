const commit = ((import.meta.env.VITE_GIT_COMMIT as string | undefined) ?? 'dev').slice(0, 7);

const footerStyle: React.CSSProperties = {
  position: 'fixed',
  bottom: 0,
  left: 0,
  right: 0,
  borderTop: '1px solid #ccc',
  background: '#fff',
  padding: '4px 16px',
  display: 'flex',
  alignItems: 'center',
  justifyContent: 'space-between',
  fontSize: '11px',
  color: '#888',
  zIndex: 1000,
  flexWrap: 'wrap',
  gap: '8px',
};

export function AppFooter() {
  return (
    <footer style={footerStyle}>
      <span>
        CountOrSell is not affiliated with, endorsed by, or sponsored by Wizards of the Coast LLC or Hasbro, Inc.
        Magic: The Gathering is a trademark of Wizards of the Coast LLC.
      </span>
      <span style={{ display: 'flex', gap: '16px', flexShrink: 0 }}>
        <span>
          Licensed under the{' '}
          <a
            href="https://www.gnu.org/licenses/agpl-3.0.html"
            target="_blank"
            rel="noopener noreferrer"
            style={{ color: '#888' }}
          >
            AGPL-3.0
          </a>
        </span>
        <span style={{ fontFamily: 'monospace' }}>build {commit}</span>
      </span>
    </footer>
  );
}
