import { useState } from 'react';
import { Link } from 'react-router-dom';
import { cardsApi, RandomFlavorResult } from '../api/cards';

type State =
  | { status: 'idle' }
  | { status: 'loading' }
  | { status: 'loaded'; card: RandomFlavorResult }
  | { status: 'empty' }
  | { status: 'error' };

export function FlavorButton() {
  const [state, setState] = useState<State>({ status: 'idle' });
  const [open, setOpen] = useState(false);

  async function handleClick() {
    if (open) {
      setOpen(false);
      setState({ status: 'idle' });
      return;
    }

    setOpen(true);
    setState({ status: 'loading' });

    try {
      const result = await cardsApi.getRandomFlavor();
      if (!result) {
        setState({ status: 'empty' });
      } else {
        setState({ status: 'loaded', card: result });
      }
    } catch {
      setState({ status: 'error' });
    }
  }

  function handleNewQuote() {
    setState({ status: 'loading' });
    cardsApi.getRandomFlavor().then((result) => {
      if (!result) {
        setState({ status: 'empty' });
      } else {
        setState({ status: 'loaded', card: result });
      }
    }).catch(() => {
      setState({ status: 'error' });
    });
  }

  return (
    <span style={{ position: 'relative', display: 'inline-block' }}>
      <button
        type="button"
        onClick={handleClick}
        aria-expanded={open}
        aria-haspopup="dialog"
      >
        Flavor Text
      </button>
      {open && (
        <div
          role="dialog"
          aria-label="Random flavor text"
          style={{
            position: 'absolute',
            top: '100%',
            left: 0,
            zIndex: 100,
            background: 'Canvas',
            border: '1px solid',
            padding: '1rem',
            minWidth: '20rem',
            maxWidth: '30rem',
          }}
        >
          {state.status === 'loading' && <p>Loading...</p>}

          {state.status === 'empty' && (
            <p>No cards with flavor text found. Flavor text arrives via content updates.</p>
          )}

          {state.status === 'error' && <p>Failed to load flavor text.</p>}

          {state.status === 'loaded' && (
            <>
              <blockquote style={{ margin: '0 0 0.75rem', fontStyle: 'italic' }}>
                {state.card.flavorText}
              </blockquote>
              <p style={{ margin: '0 0 0.75rem' }}>
                <Link to={`/cards/${state.card.identifier.toLowerCase()}`}>
                  {state.card.name} ({state.card.identifier})
                </Link>
              </p>
              <button type="button" onClick={handleNewQuote}>
                Another
              </button>
            </>
          )}

          <button
            type="button"
            onClick={() => { setOpen(false); setState({ status: 'idle' }); }}
            style={{ marginLeft: state.status === 'loaded' ? '0.5rem' : undefined }}
          >
            Close
          </button>
        </div>
      )}
    </span>
  );
}
