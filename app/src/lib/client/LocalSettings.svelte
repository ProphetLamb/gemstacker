<script lang="ts">
	import { popup, type PopupSettings } from '@skeletonlabs/skeleton';
	import type { PoeTradeLeagueResponse } from '$lib/pathOfExileApi';
	import { onMount } from 'svelte';
	import { localSettings } from './localSettings';
	import { Icon } from '@steeze-ui/svelte-icon';
	import * as hi from '@steeze-ui/heroicons';
	import { computePosition, autoUpdate, offset, shift, flip, arrow } from '@floating-ui/dom';
	import { storePopup } from '@skeletonlabs/skeleton';
	storePopup.set({ computePosition, autoUpdate, offset, shift, flip, arrow });

	export let data: { leagues: PoeTradeLeagueResponse[] };
	const pcLeagues = data.leagues.filter((l) => l.realm == 'pc');

	const localSettingsPopup: PopupSettings = {
		event: 'click',
		target: 'localSettingsPopup',
		placement: 'bottom'
	};

	onMount(() => {
		if ($localSettings.league === undefined) {
			$localSettings.league = pcLeagues[0].id;
		}
	});
</script>

<button class="btn btn-sm variant-ghost-surface hidden lg:flex" use:popup={localSettingsPopup}
	><Icon src={hi.Cog6Tooth} size="16" theme="solid" /><span>Settings</span></button
>
<button class="btn btn-sm variant-ghost-surface lg:hidden" use:popup={localSettingsPopup}
	><Icon src={hi.Cog6Tooth} size="16" theme="solid" /></button
>
<div class="">
	<div data-popup="localSettingsPopup" class="w-[calc(100%-2rem)] pr-4 lg:w-72">
		<div class="arrow bg-surface-100-800-token" />
		<div class="card flex flex-col items-center justify-start space-y-2 p-4 shadow-xl">
			<h2 class="h2">Settings</h2>
			<label class="label">
				<span>League</span>
				<select class="select" bind:value={$localSettings.league}>
					{#each pcLeagues as league}
						<option value={league.id}>{league.text}</option>
					{/each}
				</select>
			</label>
			<hr class="!border-t-2 w-full opacity-50" />
			<button class="btn variant-soft-error align-middle"
				><Icon src={hi.XMark} size="16" />
			</button>
		</div>
	</div>
</div>
