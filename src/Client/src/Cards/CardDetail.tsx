import { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { cardsApi, CardSummary } from '../api/cards';

export function CardDetail() {
  const { identifier } = useParams<{ identifier: string }>();
  const [card, setCard] = useState<CardSummary | null>(null);
  const [notFound, setNotFound] = useState(false);
  const [error, setError] = useState(false);

  useEffect(() => {
    if (!identifier) return;
    cardsApi.getByIdentifier(identifier)
      .then(setCard)
      .catch((err) => {
        if (err?.status === 404) setNotFound(true);
        else setError(true);
      });
  }, [identifier]);

  if (notFound) return <p>Card not found.</p>;
  if (error) return <p>Failed to load card.</p>;
  if (!card) return <p>Loading...</p>;

  const displayIdentifier = card.identifier.toUpperCase();

  return (
    <article>
      <h1>{card.name} <small>({displayIdentifier})</small></h1>

      {card.imageUrl && (
        <img
          src={card.imageUrl}
          alt={card.name}
          style={{ maxWidth: '250px', display: 'block', marginBottom: '1rem' }}
          onError={(e) => { e.currentTarget.style.display = 'none'; }}
        />
      )}

      <dl>
        {card.setCode && (
          <>
            <dt>Set</dt>
            <dd>{card.setCode.toUpperCase()}</dd>
          </>
        )}
        {card.cardType && (
          <>
            <dt>Type</dt>
            <dd>{card.cardType}</dd>
          </>
        )}
        {card.color && (
          <>
            <dt>Color</dt>
            <dd>{card.color}</dd>
          </>
        )}
        {card.currentMarketValue != null && (
          <>
            <dt>Market Value</dt>
            <dd>${card.currentMarketValue.toFixed(2)}</dd>
          </>
        )}
        {card.isReserved && (
          <>
            <dt>Reserved List</dt>
            <dd>Yes</dd>
          </>
        )}
      </dl>

      {card.flavorText && (
        <blockquote>
          <p style={{ fontStyle: 'italic' }}>{card.flavorText}</p>
        </blockquote>
      )}

      {card.oracleRulingUrl && (
        <p>
          <a href={card.oracleRulingUrl} target="_blank" rel="noreferrer">
            Oracle Rulings
          </a>
        </p>
      )}

      <p>
        <Link to="/collection">View in Collection</Link>
      </p>
    </article>
  );
}
