import { browser } from '$app/environment';
import { type GemProfitResponseItem } from '$lib/gemLevelProfitApi';
import { writable } from 'svelte/store';

interface InspectProfit {
	gem: GemProfitResponseItem | undefined;
	visible: boolean;
}

export const inspectProfit = writable<InspectProfit>({ gem: undefined, visible: browser && window.innerWidth > 1200 });
