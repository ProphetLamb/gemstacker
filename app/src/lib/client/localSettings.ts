import { localStorageStore } from '@skeletonlabs/skeleton';
import { derived } from 'svelte/store';
import { leagues } from './leagues';
import { gemProfitRequestParameterConstraints } from '$lib/gemLevelProfitApi';
import type { PoeTradeLeagueResponse } from '$lib/pathOfExileApi';

export interface LocalSettings {
	league: string;
	min_experience_delta: number;
	exclude_gems: string[];
	min_listing_count: number;
}

function localSettingsStore() {
	const storage = localStorageStore<LocalSettings>(
		'poe-gemleveling-profit-calculator-local-settings',
		{
			league: '',
			min_experience_delta: gemProfitRequestParameterConstraints.min_experience_delta.defaultValue,
			exclude_gems: [],
			min_listing_count: 8
		}
	);
	const reader = derived([storage, leagues], ([storage, leagues]) => {
		const { league, min_experience_delta, exclude_gems, min_listing_count } = storage;
		return {
			min_experience_delta: normalizeMinExperienceDelta(min_experience_delta),
			league: normalizeLeague(league, leagues),
			exclude_gems: normalizeExcludeGems(exclude_gems),
			min_listing_count: normalizeMinListingsCount(min_listing_count)
		};
	});
	return {
		subscribe: reader.subscribe,
		set: storage.set,
		update: storage.update
	};

	function normalizeExcludeGems(exclude_gems: string[]) {
		return [...new Set(exclude_gems)];
	}

	function normalizeLeague(league: string, leagues: PoeTradeLeagueResponse[]) {
		if (leagues.length !== 0 && !leagues.find((l) => l.id == league)) {
			return leagues[0].id;
		}
		return league;
	}

	function normalizeMinExperienceDelta(min_experience_delta?: number) {
		const constraints = gemProfitRequestParameterConstraints.min_experience_delta;
		if (!min_experience_delta) {
			return constraints.defaultValue;
		}
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

	function normalizeMinListingsCount(min_listing_count?: number) {
		const constraints = gemProfitRequestParameterConstraints.min_listing_count;
		if (!min_listing_count) {
			return constraints.defaultValue;
		}
		const steppedDelta = Math.floor(min_listing_count / constraints.step) * constraints.step;
		if (steppedDelta !== min_listing_count) {
			return steppedDelta;
		}
		if (min_listing_count < constraints.min) {
			return constraints.min;
		}
		if (min_listing_count > constraints.max) {
			return constraints.max;
		}
		return min_listing_count;
	}
}

export const localSettings = localSettingsStore();
