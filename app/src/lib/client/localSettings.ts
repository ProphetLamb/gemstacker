import { localStorageStore } from '@skeletonlabs/skeleton';
import { derived } from 'svelte/store';
import { leagues } from './leagues';
import { gemProfitRequestParameterConstraints } from '$lib/gemLevelProfitApi';
import type { PoeTradeLeagueResponse } from '$lib/pathOfExileApi';

export interface LocalSettings {
	league: string;
	min_experience_delta: number;
	exclude_gems: string[];
}

function localSettingsStore() {
	const storage = localStorageStore<LocalSettings>(
		'poe-gemleveling-profit-calculator-local-settings',
		{
			league: '',
			min_experience_delta: gemProfitRequestParameterConstraints.min_experience_delta.defaultValue,
			exclude_gems: []
		}
	);
	const reader = derived([storage, leagues], ([storage, leagues]) => {
		const { league, min_experience_delta, exclude_gems } = storage;
		return {
			min_experience_delta: normalizeMinExperienceDelta(min_experience_delta),
			league: normalizeLeague(league, leagues),
			exclude_gems: normalizeExcludeGems(exclude_gems)
		};
	});
	return {
		subscribe: reader.subscribe,
		set: storage.set,
		update: storage.update
	};

	function normalizeExcludeGems(exclude_gems: string[]) {
    return [...new Set(exclude_gems)]
  }

	function normalizeLeague(league: string, leagues: PoeTradeLeagueResponse[]) {
		if (leagues.length !== 0 && !leagues.find((l) => l.id == league)) {
			return leagues[0].id;
		}
		return league;
	}

	function normalizeMinExperienceDelta(min_experience_delta: number) {
		const constraints = gemProfitRequestParameterConstraints.min_experience_delta;
		const steppedDelta = Math.floor(min_experience_delta / constraints.step) * constraints.step;
		if (steppedDelta !== min_experience_delta) {
			return steppedDelta;
		}
		if (min_experience_delta < constraints.min) {
			return constraints.min;
		}
		if (min_experience_delta > constraints.max) {
			return constraints.max;
		}
		return min_experience_delta;
	}
}

export const localSettings = localSettingsStore();
