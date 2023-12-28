import { writable } from "svelte/store";
import type { GemProfitResponseItem } from "$lib/gemLevelProfitApi"

export type AvailableGem = GemProfitResponseItem & { isFiltered?: boolean }

export const availableGems = writable<AvailableGem[] | undefined>(undefined)

export type FilteredEvent = {
  dataIndex: number[];
  gem: AvailableGem[];
  oldValue: (boolean | undefined)[];
  newValue: boolean | undefined;
} | {
  dataIndex: number;
  gem: AvailableGem;
  oldValue: boolean | undefined;
  newValue: boolean | undefined;
};
