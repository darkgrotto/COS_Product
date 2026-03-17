interface Props {
  inline?: boolean;
}

// Badge shown on Reserved List cards.
// Uses a gold/amber color to visually distinguish from condition or other badges.
export function ReservedBadge({ inline = true }: Props) {
  return (
    <abbr
      title="Reserved List - Wizards of the Coast has committed to never reprint this card"
      style={{
        display: inline ? 'inline-block' : 'block',
        marginLeft: inline ? '6px' : undefined,
        padding: '1px 5px',
        fontSize: '10px',
        fontWeight: 700,
        fontStyle: 'normal',
        lineHeight: '16px',
        borderRadius: '3px',
        backgroundColor: '#92400e',
        color: '#fef3c7',
        verticalAlign: 'middle',
        cursor: 'help',
        textDecoration: 'none',
        whiteSpace: 'nowrap',
      }}
    >
      RL
    </abbr>
  );
}
