interface SetSymbolProps {
  setCode: string;
  'aria-hidden'?: boolean;
}

/**
 * Renders a Magic: The Gathering set symbol using the Keyrune font.
 * setCode should be the lowercase set code (e.g. "eoe", "3ed").
 */
export function SetSymbol({ setCode, 'aria-hidden': ariaHidden = true }: SetSymbolProps) {
  return (
    <i
      className={`ss ss-${setCode.toLowerCase()}`}
      aria-hidden={ariaHidden}
    />
  );
}
