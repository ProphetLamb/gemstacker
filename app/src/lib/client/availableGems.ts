import { writable } from "svelte/store";
import type { GemProfitResponseItem } from "$lib/gemLevelProfitApi"

export type AvailableGem = GemProfitResponseItem & { isFiltered?: boolean }

export const availableGems = writable<AvailableGem[] | undefined>(undefined)