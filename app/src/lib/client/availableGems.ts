import { writable } from "svelte/store";
import type { GemProfitResponse, GemProfitResponseItem } from "$lib/gemLevelProfitApi"

export const availableGems = writable<GemProfitResponse | undefined>(undefined)

export type FilteredEvent = {
  filtered: FilteredEventData
}

export type FilteredEventData = {
  dataIndex: number[];
  gem: GemProfitResponse;
  oldValue: boolean[];
  newValue: boolean;
} | {
  dataIndex: number;
  gem: GemProfitResponseItem;
  oldValue: boolean;
  newValue: boolean;
};