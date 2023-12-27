import { writable } from "svelte/store";
import type { GemProfitResponse } from "$lib/gemLevelProfitApi"

export const availableGems = writable<GemProfitResponse | undefined>(undefined)