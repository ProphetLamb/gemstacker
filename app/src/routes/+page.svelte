<script lang="ts">
	import { Wrapper, WrapperItem } from '$lib/client/wrapper';
	import { loadoutRequestSchema } from '$lib/loadout';
	import { message, superForm, superValidateSync } from 'sveltekit-superforms/client';
	import * as hi from '@steeze-ui/heroicons';
	import { Icon } from '@steeze-ui/svelte-icon';
	import { locationWithSearch } from '$lib/client/navigation';
	import * as pob from '$lib/pathOfBuilding';
	import { goto } from '$app/navigation';
	import LoadingPlaceholder from '$lib/client/LoadingPlaceholder.svelte';
	import { ProgressRadial } from '@skeletonlabs/skeleton';
	import GemProfitTable from '$lib/client/GemProfitTable.svelte';
	import { gemProfitRequestParameterSchema, type GemProfitRequestParameter, type GemProfitResponse, type GemProfitResponseItem } from '$lib/gemLevelProfitApi';
	import { localSettings } from '$lib/client/localSettings';
	import BetterTrading from '$lib/client/BetterTrading.svelte';
	import MetaHead from '$lib/client/MetaHead.svelte';
	import type { ProfitPreviewResponse } from './api/profit-preview/+server.js';
	import { objectToQueryParams } from '$lib/url.js';
	import { getFlash } from 'sveltekit-flash-message';
	import { page } from '$app/stores';
	import type { ToastMessage } from '$lib/toast.js';
	import { exchangeRates } from '$lib/client/exchangeRates.js';

	export let data

	const flash = getFlash(page)

	let pobText: string = '';
	$: pobError = '';

	const {
		form: loadoutForm,
		errors: loadoutErrors,
		constraints: loadoutConstraints
	} = superForm(superValidateSync(loadoutRequestSchema), {
		validators: loadoutRequestSchema,
		taintedMessage: null
	});

	const {
		form: profitForm,
		errors: profitErrors,
		constraints: profitConstraints
	} = superForm(superValidateSync(gemProfitRequestParameterSchema), {
		validators: gemProfitRequestParameterSchema,
		taintedMessage: null
	})

	$: loadoutHref = locationWithSearch(
		{
			red: $loadoutForm.red,
			green: $loadoutForm.green,
			blue: $loadoutForm.blue,
			white: $loadoutForm.white
		},
		'/loadout'
	).pathSearchHash()

	$: profitHref = locationWithSearch(
		{
			gem_name: $profitForm.gem_name
		},
		'/single'
	).pathSearchHash()

	$: profitPreview = getProfitPreview()

	async function getProfitPreview(): Promise<ProfitPreviewResponse | undefined> {
		const query = objectToQueryParams({
			league: $localSettings.league,
			min_experience_delta: $localSettings.min_experience_delta,
			min_listing_count: $localSettings.min_listing_count,
		} satisfies GemProfitRequestParameter)
		const rsp = await fetch(`/api/profit-preview?${query}`)
		if (rsp.status < 200 || rsp.status >= 300) {
			console.log('/:getProfitPreview', await rsp.text())
			$flash = { message: 'Failed to load profit preview', type: 'error'} satisfies ToastMessage
			return undefined
		}
		const content: ProfitPreviewResponse = await rsp.json()
		$exchangeRates = content.exchange_rates ?? $exchangeRates
		return content
	}

	async function gotoPob() {
		try {
			if (!pobText) {
				throw Error('Cannot be empty');
			}
			const xml = pob.deserializePob(pobText);
			const sockets = pob.availableSockets(xml);
			const url = locationWithSearch(sockets, '/loadout').pathSearchHash();
			goto(url);
		} catch (err) {
			console.log('/:gotoPob', err)
			if (err instanceof Error) {
				pobError = err.message;
			} else {
				pobError = 'Invalid build code';
			}
		}
	}
</script>

<div class="flex flex-col justify-center">
	<div class="w-full align-middle flex justify-center p-4">
		<h1 class="h1 flex flex-row items-center space-x-4 pb-4">
			<Icon src={hi.Sparkles} theme="solid" class=" text-yellow-300" size="32" />
			<span>
			The best
			<span
				class="bg-clip-text shadow-lime-700 text-transparent bg-gradient-to-tr from-lime-700 to-yellow-600 via-accent animate-gradient-xy font-bold"
				>gems</span
			></span>
		</h1>
	</div>
	<div class="flex flex-col items-center">
		{#await profitPreview}
			<div class="text-token bg-surface-100/75 dark:bg-surface-800/90 card p-4 space-y-2">
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
			</div>
		{:then profitPreview}
		{#if profitPreview}
			<div class="text-token flex flex-col items-center bg-surface-100/75 dark:bg-surface-800/90 card p-4 space-y-2">
				<GemProfitTable data={profitPreview.gem_profit} />
				<BetterTrading data={profitPreview.gem_profit} />
			</div>
		{/if}
		{/await}
	</div>
</div>
<Wrapper>
	<WrapperItem>
		<h2 class="h2">&#133;for your perusal.</h2>
			<label class="label">
			<span>Gem Name</span>
			<input
				name="gem_name"
				class="input"
				type="text"
				placeholder="Gem name glob..."
				bind:value={$profitForm.gem_name}
				{...$profitConstraints.gem_name}
			/>
			{#if $profitErrors.gem_name}
				<aside class="alert variant-glass-error">{$profitErrors.gem_name}</aside>
			{/if}
		</label>
		<a href={profitHref} class="btn variant-filled-primary shadow-lg text-2xl relative">
			<Icon src={hi.ArrowTopRightOnSquare} size="22" />
			<span class="mr-0.5">Search</span>
			<div class="btn variant-filled-warning absolute -top-4 left-12 py-0.5 px-1 text-center justify-center flex flex-row text-sm">
				<Icon src={hi.Scale} size="16" />
				Advanced
			</div>
		</a>
	</WrapperItem>
	<div class="divider">
		<div>&nbsp;</div>
		<span class="text-3xl py-2">OR</span>
		<div>&nbsp;</div>
	</div>
	<WrapperItem>
		<h2 class="h2">&#133;for your loadout.</h2>
		<div class="space-y-2">
			<label class="label">
				<span>Red Sockets</span>
				<input
					class="input"
					type="number"
					name="red"
					bind:value={$loadoutForm.red}
					{...$loadoutConstraints.red}
				/>
				{#if $loadoutErrors.red}
					<aside class="alert variant-glass-error">{$loadoutErrors.red}</aside>
				{/if}
			</label>
			<label class="label">
				<span>Green Sockets</span>
				<input
					class="input"
					type="number"
					name="green"
					bind:value={$loadoutForm.green}
					{...$loadoutConstraints.green}
				/>
				{#if $loadoutErrors.green}
					<aside class="alert variant-glass-error">{$loadoutErrors.green}</aside>
				{/if}
			</label>
			<label class="label">
				<span>Blue Sockets</span>
				<input
					class="input"
					type="number"
					name="blue"
					bind:value={$loadoutForm.blue}
					{...$loadoutConstraints.blue}
				/>
				{#if $loadoutErrors.blue}
					<aside class="alert variant-glass-error">{$loadoutErrors.blue}</aside>
				{/if}
			</label>
			<label class="label">
				<span>White Sockets</span>
				<input
					class="input"
					type="number"
					name="white"
					bind:value={$loadoutForm.white}
					{...$loadoutConstraints.white}
				/>
				{#if $loadoutErrors.white}
					<aside class="alert variant-glass-error">{$loadoutErrors.white}</aside>
				{/if}
			</label>
		</div>
		<a href={loadoutHref} class="btn variant-filled-primary shadow-lg text-2xl">
			<Icon src={hi.ArrowTopRightOnSquare} size="22" />
			<span class="mr-0.5">Loadout</span>
		</a>
	</WrapperItem>
	<div class="divider">
		<div>&nbsp;</div>
		<span class="text-3xl py-2">OR</span>
		<div>&nbsp;</div>
	</div>
	<WrapperItem>
		<h2 class="h2">&#133;for your build.</h2>
		<label class="label">
			<span>Build code</span>
			<input class="input" type="text" name="red" bind:value={pobText} />
			{#if pobError}
				<aside class="alert variant-glass-error">{pobError}</aside>
			{/if}
		</label>

		<button class="btn variant-filled-primary shadow-lg text-2xl" on:click={gotoPob}>
			<Icon src={hi.ArrowTopRightOnSquare} size="22" />
			<span class="mr-0.5">Build</span>
		</button>
	</WrapperItem>
</Wrapper>

<style lang="postcss">
	.divider {
		@apply flex items-center space-x-2 md:space-x-0 md:flex-col sm:flex-row w-full md:w-fit px-4 md:px-0 py-0 md:py-4  md:h-[30rem] pt-[4rem];

		& > div {
			@apply h-0.5 md:h-full w-full md:w-0.5 bg-surface-200-700-token opacity-20 rounded-full;
		}
	}
</style>

<svelte:head>
	<MetaHead request_url={data.request_url} />
</svelte:head>