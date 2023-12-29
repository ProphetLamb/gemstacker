
import type { PoeTradeLeagueResponse } from '$lib/pathOfExileApi';
import { writable } from 'svelte/store';


export const leagues = writable<PoeTradeLeagueResponse[]>([])