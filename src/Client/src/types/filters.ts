export type CardCondition = 'NM' | 'LP' | 'MP' | 'HP' | 'DMG';

export interface CollectionFilter {
  setCode?: string;
  color?: string;
  condition?: CardCondition;
  cardType?: string;
  treatment?: string;
  autographed?: boolean;
  serialized?: boolean;
  slabbed?: boolean;
  sealedProduct?: boolean;
  gradingAgency?: string;
  sealedCategorySlug?: string;
  sealedSubTypeSlug?: string;
}

export const CARD_CONDITIONS: CardCondition[] = ['NM', 'LP', 'MP', 'HP', 'DMG'];

export const CARD_CONDITION_LABELS: Record<CardCondition, string> = {
  NM: 'Near Mint',
  LP: 'Lightly Played',
  MP: 'Moderately Played',
  HP: 'Heavily Played',
  DMG: 'Damaged',
};

export const CARD_COLORS = [
  'White',
  'Blue',
  'Black',
  'Red',
  'Green',
  'Colorless',
  'Multicolor',
];

export const CARD_TYPES = [
  'Creature',
  'Instant',
  'Sorcery',
  'Land',
  'Enchantment',
  'Artifact',
  'Planeswalker',
  'Battle',
];
