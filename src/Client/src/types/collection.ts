import { CardCondition } from './filters';

export interface CollectionEntry {
  id: string;
  userId: string;
  cardIdentifier: string;
  treatment: string;
  quantity: number;
  condition: CardCondition;
  autographed: boolean;
  acquisitionDate: string;
  acquisitionPrice: number;
  notes: string | null;
  currentMarketValue: number | null;
}

export interface SerializedEntry {
  id: string;
  userId: string;
  cardIdentifier: string;
  treatment: string;
  serialNumber: number;
  printRunTotal: number;
  condition: CardCondition;
  autographed: boolean;
  acquisitionDate: string;
  acquisitionPrice: number;
  notes: string | null;
  currentMarketValue: number | null;
}

export interface SlabEntry {
  id: string;
  userId: string;
  cardIdentifier: string;
  treatment: string;
  gradingAgencyCode: string;
  grade: string;
  certificateNumber: string;
  serialNumber: number | null;
  printRunTotal: number | null;
  acquisitionDate: string;
  acquisitionPrice: number;
  notes: string | null;
  currentMarketValue: number | null;
}

export interface SealedInventoryEntry {
  id: string;
  userId: string;
  productIdentifier: string;
  quantity: number;
  condition: CardCondition;
  acquisitionDate: string;
  acquisitionPrice: number;
  notes: string | null;
  currentMarketValue: number | null;
}

export interface WishlistEntry {
  id: string;
  userId: string;
  cardIdentifier: string;
  currentMarketValue: number | null;
}
