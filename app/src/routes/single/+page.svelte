<script lang="ts">
	import { ProgressRadial } from '@skeletonlabs/skeleton';
	import { superForm } from 'sveltekit-superforms/client';
	import type { ActionData, PageData } from './$types';
	import { gemProfitRequestParameterSchema } from '$lib/gemLevelProfitApi';
	import { Icon } from '@steeze-ui/svelte-icon';
	import * as hi from '@steeze-ui/heroicons';
	import GemProfitTable from '$lib/client/GemProfitTable.svelte';
	import { localSettings } from '$lib/client/localSettings';
	import AnimatedSearchButton from '$lib/client/AnimatedSearchButton.svelte';
	import { Wrapper, WrapperItem } from '$lib/client/wrapper';
	import LoadingPlaceholder from '$lib/client/LoadingPlaceholder.svelte';
	import { intlCompactNumber } from '$lib/intl';
	import { onMount } from 'svelte';
	import BetterTrading from '$lib/client/BetterTrading.svelte';
	import { getStateFromQuery, replaceStateWithQuery } from '$lib/client/navigation';
	import { browser } from '$app/environment';
	import GemFilter from '$lib/client/GemFilter.svelte';
	import { availableGems } from '$lib/client/availableGems';
	import GemProfitTableHeader from '$lib/client/GemProfitTableHeader.svelte';
	import { exchangeRates } from '$lib/client/exchangeRates';

	export let data: PageData;
	export let form: ActionData;
	const {
		form: profitForm,
		capture,
		restore,
		errors,
		constraints,
		enhance,
		delayed
	} = superForm(data.gemLevelsProfitForm, {
		validators: gemProfitRequestParameterSchema,
		taintedMessage: null
	});

	$: $availableGems = form?.gem_profit;
	$: $exchangeRates = form?.exchange_rates ?? $exchangeRates;
	$: excludedGems = new Set($localSettings.exclude_gems);
	$: gemProfit =
		$delayed || !$availableGems
			? undefined
			: !excludedGems
			? $availableGems
			: $availableGems.filter((x) => !excludedGems.has(x.name.toLowerCase()));

	export const snapshot = { capture, restore };

	let htmlProfitForm: HTMLFormElement;

	function fillFormFromQuery() {
		function num(s?: string): number | undefined {
			try {
				return !!s ? parseInt(s) : undefined;
			} catch (err) {
				console.log('/single:fillFormFromQuery.num', err);
				return undefined;
			}
		}
		const initialSettings = {
			league: $localSettings.league,
			min_experience_delta: $localSettings.min_experience_delta,
			min_listing_count: $localSettings.min_listing_count
		};
		if (!$availableGems) {
			$profitForm = {
				...$profitForm,
				...initialSettings,
				...getStateFromQuery((x) => {
					return {
						league: x['league'],
						gem_name: x['gem_name'],
						added_quality: num(x['added_quality']),
						min_sell_price_chaos: num(x['min_sell_price_chaos']),
						max_buy_price_chaos: num(x['max_buy_price_chaos']),
						min_experience_delta: num(x['min_experience_delta'])
					};
				})
			};
		}
	}

	function shouldAutoRequestForm() {
		return !!$profitForm.gem_name && $profitForm.gem_name.trim().length > 0;
	}

	function submitFormFilledFromQuery() {
		// remove query parameters
		replaceStateWithQuery({
			gem_name: undefined
		});
		// submit form filled via query
		htmlProfitForm.requestSubmit();
	}
	if (browser) {
		fillFormFromQuery();
	}
	onMount(() => {
		if (browser && shouldAutoRequestForm()) {
			submitFormFilledFromQuery();
		}
	});
</script>

<Wrapper>
	<WrapperItem>
		<h1 class="h1">
			<span class="text-shadow shadow-surface-500"> Gem levels for </span><span
				class="bg-clip-text shadow-surface-500 text-transparent bg-gradient-to-tr from-indigo-500 to-sky-300 via-accent animate-gradient-xy font-bold"
				>profit</span
			>.
		</h1>
		<form class="space-y-2" bind:this={htmlProfitForm} use:enhance method="POST">
			<label class="label">
				<span>League</span>
				<select
					class="select rounded-full"
					name="league"
					bind:value={$profitForm.league}
					{...$constraints.league}
				>
					{#each data.leagues.filter((l) => l.realm === 'pc') as league}
						<option value={league.id}>{league.text}</option>
					{/each}
				</select>
				{#if $errors.league}
					<aside class="alert variant-glass-error">{$errors.league}</aside>
				{/if}
			</label>
			<label class="label">
				<span>Gem Name</span>
				<input
					name="gem_name"
					class="input"
					type="text"
					placeholder="Gem name glob..."
					bind:value={$profitForm.gem_name}
					{...$constraints.gem_name}
				/>
				{#if $errors.gem_name}
					<aside class="alert variant-glass-error">{$errors.gem_name}</aside>
				{/if}
			</label>
			<label class="label">
				<span>Added Quality</span>
				<input
					name="added_quality"
					class="input"
					type="number"
					placeholder="Quality fom Sockets..."
					bind:value={$profitForm.added_quality}
					{...$constraints.added_quality}
				/>
				{#if $errors.added_quality}
					<aside class="alert variant-glass-error">{$errors.added_quality}</aside>
				{/if}
			</label>
			<label class="label">
				<span>Sell price (minimum Chaos value)</span>
				<input
					name="min_sell_price_chaos"
					class="input"
					type="number"
					placeholder="Minimum sell price..."
					bind:value={$profitForm.min_sell_price_chaos}
					{...$constraints.min_sell_price_chaos}
				/>
				{#if $errors.min_sell_price_chaos}
					<aside class="alert variant-glass-error">{$errors.min_sell_price_chaos}</aside>
				{/if}
			</label>
			<label class="label">
				<span>Buy price (maximum Chaos value)</span>
				<input
					name="max_buy_price_chaos"
					class="input"
					type="number"
					placeholder="Maximum buy price..."
					bind:value={$profitForm.max_buy_price_chaos}
					{...$constraints.max_buy_price_chaos}
				/>
				{#if $errors.max_buy_price_chaos}
					<aside class="alert variant-glass-error">{$errors.max_buy_price_chaos}</aside>
				{/if}
			</label>
			<label class="label">
				<span>Minimum experience required for leveling</span>
				<input
					name="min_experience_delta"
					class="input"
					type="range"
					bind:value={$profitForm.min_experience_delta}
					{...$constraints.min_experience_delta}
				/>
				<p>
					<span class="text-token"
						>+{intlCompactNumber.format($profitForm.min_experience_delta)}</span
					><span class="text-sm text-surface-600-300-token">exp</span>
				</p>
				{#if $errors.min_experience_delta}
					<aside class="alert variant-glass-error">{$errors.min_experience_delta}</aside>
				{/if}
			</label>
			<label class="label">
				<span>Minimum number of listings for gem</span>
				<input
					name="min_listing_count"
					class="input"
					type="range"
					bind:value={$profitForm.min_listing_count}
					{...$constraints.min_listing_count}
				/>
				<p>
					<span class="text-token"
						>{intlCompactNumber.format($profitForm.min_listing_count || 0)}</span
					><span class="text-sm text-surface-600-300-token">&nbsp;listing</span>
				</p>
				{#if $errors.min_listing_count}
					<aside class="alert variant-glass-error">{$errors.min_listing_count}</aside>
				{/if}
			</label>
			<AnimatedSearchButton type="submit" class="shadow-lg text-2xl">
				<Icon src={hi.MagnifyingGlass} size="22" />
				<span class="mr-0.5">Search</span></AnimatedSearchButton
			>
		</form>
	</WrapperItem>
	<WrapperItem>
		<h1 class="h1 flex flex-row items-center space-x-4 pb-4 text-shadow shadow-surface-500">
			<Icon src={hi.Sparkles} theme="solid" class=" text-yellow-300" size="32" />
			<span>The best gems for you.</span>
		</h1>
		<div class="flex flex-col items-center">
			<div class="text-token card p-4 space-y-2">
				{#if $delayed}
					<LoadingPlaceholder
						class="w-[51rem] max-w-[calc(100vw-4rem)]"
						front="bg-surface-backdrop-token"
						placeholder="animate-pulse"
						rows={10}
					>
						<ProgressRadial
							stroke={100}
							value={undefined}
							meter="stroke-tertiary-500"
							track="stroke-tertiary-500/30"
						/>
						<p class="text-xl">Loading...</p></LoadingPlaceholder
					>
				{:else if gemProfit && gemProfit.length > 0}
					<GemProfitTableHeader>
						<GemFilter slot="buttons" />
					</GemProfitTableHeader>
					<GemProfitTable data={gemProfit} />
					<BetterTrading data={gemProfit} />
				{:else}
					<GemProfitTableHeader>
						<svelte:fragment slot="buttons">
							{#if $availableGems && $availableGems.length > 0}
								<GemFilter />
							{/if}
						</svelte:fragment>
					</GemProfitTableHeader>
					<LoadingPlaceholder
						class="w-[51rem] max-w-[calc(100vw-4rem)]"
						front="bg-surface-backdrop-token"
						rows={10}
					>
						<p class="text-xl">
							Enter your criteria or just <span class="font-extrabold">search</span>
						</p>
					</LoadingPlaceholder>
				{/if}
			</div>
		</div>
	</WrapperItem>
</Wrapper>

<svelte:head>
	<meta property="og:title" content="Gem Stacker - The best gems for your perusal" />
	<meta property="og:type" content="website" />
	<meta property="og:url" content={data.request_url} />
	<meta property="og:locale" content="en-US" />
	<meta property="og:locale:alternate" content="en-GB" />
</svelte:head>

<style lang="postcss">
</style>
