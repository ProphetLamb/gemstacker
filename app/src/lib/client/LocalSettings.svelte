<script lang="ts">
	import { popup, type CssClasses, type PopupSettings } from '@skeletonlabs/skeleton';
	import { localSettings } from './localSettings';
	import { Icon } from '@steeze-ui/svelte-icon';
	import * as hi from '@steeze-ui/heroicons';
	import { intlCompactNumber } from '$lib/intl';
	import { createEventDispatcher } from 'svelte';
	import { deepEqual } from '$lib/compare';
	import { leagues } from './leagues';
	import { gemProfitRequestParameterConstraints } from '$lib/gemLevelProfitApi';
	import GemFilter from './GemFilter.svelte';
	import { availableGems } from './availableGems';
	import { exchangeRates } from './exchangeRates';
	import ExchangeRates from './ExchangeRates.svelte';
	import { currencyDisplayValues } from './currency';

	export let textClass: CssClasses = '';

	const initialLocalSettings = { ...$localSettings };
	const pcLeagues = $leagues.filter((l) => l.realm == 'pc');

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

<button
	class="btn btn-sm variant-ghost-tertiary {$$props.class ?? ''}"
	use:popup={localSettingsPopup}
	><Icon src={hi.Cog6Tooth} size="16" theme="solid" /><span class={textClass}>Settings</span
	></button
>
<div class="">
	<div data-popup="localSettingsPopup" class="w-[calc(100%)] sm:w-[calc(100%-2rem)] pr-4 md:w-96">
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
					{...gemProfitRequestParameterConstraints.min_experience_delta}
					bind:value={$localSettings.min_experience_delta}
				/>
				<p>
					<span class="text-token"
						>+{intlCompactNumber.format($localSettings.min_experience_delta)}</span
					><span class="text-sm text-surface-600-300-token">exp</span>
				</p>
			</label>
			<label class="label">
				<span>Minimum number of listings for gem</span>
				<input name="min_listing_count"
				class="input"
				type="range"
				bind:value={$localSettings.min_listing_count}
				{...gemProfitRequestParameterConstraints.min_listing_count}
				/>
				<p>
					<span class="text-token"
						>{intlCompactNumber.format($localSettings.min_listing_count || 0)}</span
					><span class="text-sm text-surface-600-300-token">&nbsp;listing</span>
				</p>
			</label>
			<label class="label">
				<span>Display currency</span>
				<select name="currency_display" class="select" bind:value={$localSettings.currency_display}>
					{#each Object.entries(currencyDisplayValues) as [currencyDisplay, text]}
						<option value="{currencyDisplay}">{text}</option>
					{/each}
				</select>
			</label>
			{#if $exchangeRates}
				<ExchangeRates />
			{/if}
			{#if $availableGems && $availableGems.length > 0}
				<GemFilter />
			{/if}
			<hr class="!border-t-2 w-full opacity-50" />
			<button class="btn variant-soft-error align-middle"
				><Icon src={hi.XMark} size="16" />
			</button>
		</div>
	</div>
</div>
