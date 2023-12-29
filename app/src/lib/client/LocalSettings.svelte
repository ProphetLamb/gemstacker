<script lang="ts">
	import { popup, type PopupSettings } from '@skeletonlabs/skeleton';
	import type { PoeTradeLeagueResponse } from '$lib/pathOfExileApi';
	import { localSettings } from './localSettings';
	import { Icon } from '@steeze-ui/svelte-icon';
	import * as hi from '@steeze-ui/heroicons';
	import { intlCompactNumber } from '$lib/intl';
	import { createEventDispatcher } from 'svelte';
	import { deepEqual } from '$lib/compare';

	export let data: { leagues: PoeTradeLeagueResponse[] };
	const initialLocalSettings = { ...$localSettings };
	const pcLeagues = data.leagues.filter((l) => l.realm == 'pc');

	const dispatch = createEventDispatcher();

	const localSettingsPopup: PopupSettings = {
		event: 'click',
		target: 'localSettingsPopup',
		placement: 'bottom',
		state: ({ state }) => {
			if (!state) {
				dispatch('close', { changed: !deepEqual($localSettings, initialLocalSettings) });
			} else {
				dispatch('open');
			}
		}
	};
</script>

<button class="btn btn-sm variant-ghost-tertiary hidden lg:flex" use:popup={localSettingsPopup}
	><Icon src={hi.Cog6Tooth} size="16" theme="solid" /><span>Settings</span></button
>
<button class="btn btn-sm variant-ghost-tertiary lg:hidden" use:popup={localSettingsPopup}
	><Icon src={hi.Cog6Tooth} size="16" theme="solid" /></button
>
<div class="">
	<div data-popup="localSettingsPopup" class="w-[calc(100%-2rem)] pr-4 md:w-96">
		<div class="arrow bg-surface-100-800-token" />
		<div class="card flex flex-col items-stretch justify-start space-y-2 p-4 shadow-xl">
			<h2 class="h2">Settings</h2>
			<label class="label">
				<span>League</span>
				<select class="select" bind:value={$localSettings.league}>
					{#each pcLeagues as league}
						<option value={league.id}>{league.text}</option>
					{/each}
				</select>
			</label>
			<label class="label">
				<span>Minimum experience required for leveling</span>
				<input
					name="min_experience_delta"
					class="input"
					type="range"
					step={5000000}
					min={200000000}
					max={2000000000}
					bind:value={$localSettings.min_experience_delta}
				/>
				<p>
					<span class="text-token"
						>+{intlCompactNumber.format($localSettings.min_experience_delta)}</span
					><span class="text-sm text-surface-600-300-token">exp</span>
				</p>
			</label>
			<hr class="!border-t-2 w-full opacity-50" />
			<button class="btn variant-soft-error align-middle"
				><Icon src={hi.XMark} size="16" />
			</button>
		</div>
	</div>
</div>
