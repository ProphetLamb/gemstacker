<script lang="ts">
	import { Wrapper, WrapperItem } from '$lib/client/wrapper';
	import { loadoutRequestSchema } from '$lib/loadout';
	import { superForm, superValidateSync } from 'sveltekit-superforms/client';
	import * as hi from '@steeze-ui/heroicons';
	import { Icon } from '@steeze-ui/svelte-icon';
	import { locationWithSearch } from '$lib/client/navigation';
	import * as pob from '$lib/pathOfBuilding';
	import { goto } from '$app/navigation';
	import LoadingPlaceholder from '$lib/client/LoadingPlaceholder.svelte';
	import { ProgressRadial } from '@skeletonlabs/skeleton';
	import GemProfitTable from '$lib/client/GemProfitTable.svelte';
	import type { GemProfitResponse, GemProfitResponseItem } from '$lib/gemLevelProfitApi';
	import { localSettings } from '$lib/client/localSettings';

	let pobText: string = '';
	$: pobError = '';

	const {
		form: loadoutForm,
		errors,
		constraints
	} = superForm(superValidateSync(loadoutRequestSchema), {
		validators: loadoutRequestSchema,
		taintedMessage: null
	});

	$: loadoutHref = locationWithSearch(
		{
			red: $loadoutForm.red,
			green: $loadoutForm.green,
			blue: $loadoutForm.blue,
			white: $loadoutForm.white
		},
		'/loadout'
	).pathSearchHash();

	$: gemProfit = getProfitPreview()

	async function getProfitPreview(): Promise<GemProfitResponseItem[] | undefined> {
		const query = new URLSearchParams();
		query.set('league', $localSettings.league)
		query.set('min_experience_delta', $localSettings.min_experience_delta.toString())
		const rsp = await fetch(`/api/profit-preview?${query}`)
		const content: GemProfitResponse | string = await rsp.json()
		if (Array.isArray(content)) {
			return content
		}

		console.log(content)
		return undefined
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
		} catch (e) {
			if (e instanceof Error) {
				pobError = e.message;
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
		{#await gemProfit}
			<div class="text-token bg-surface-100/50 dark:bg-surface-800/50 card p-4 space-y-2">
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
		{:then gemProfit}
		{#if gemProfit}
			<div class="text-token bg-surface-100/50 dark:bg-surface-800/50 card p-4 space-y-2">
				<GemProfitTable data={gemProfit} />
			</div>
		{/if}
		{/await}
	</div>
</div>
<Wrapper>
	<WrapperItem>
		<h2 class="h2">&#133;for your build</h2>
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
	<div class="divider">
		<div>&nbsp;</div>
		<span class="text-3xl py-2">OR</span>
		<div>&nbsp;</div>
	</div>
	<WrapperItem>
		<h2 class="h2">&#133;for your loadout</h2>
		<div class="space-y-2">
			<label class="label">
				<span>Red Sockets</span>
				<input
					class="input"
					type="number"
					name="red"
					bind:value={$loadoutForm.red}
					{...$constraints.red}
				/>
				{#if $errors.red}
					<aside class="alert variant-glass-error">{$errors.red}</aside>
				{/if}
			</label>
			<label class="label">
				<span>Green Sockets</span>
				<input
					class="input"
					type="number"
					name="green"
					bind:value={$loadoutForm.green}
					{...$constraints.green}
				/>
				{#if $errors.green}
					<aside class="alert variant-glass-error">{$errors.green}</aside>
				{/if}
			</label>
			<label class="label">
				<span>Blue Sockets</span>
				<input
					class="input"
					type="number"
					name="blue"
					bind:value={$loadoutForm.blue}
					{...$constraints.blue}
				/>
				{#if $errors.blue}
					<aside class="alert variant-glass-error">{$errors.blue}</aside>
				{/if}
			</label>
			<label class="label">
				<span>White Sockets</span>
				<input
					class="input"
					type="number"
					name="white"
					bind:value={$loadoutForm.white}
					{...$constraints.white}
				/>
				{#if $errors.white}
					<aside class="alert variant-glass-error">{$errors.white}</aside>
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
		<h2 class="h2">&#133;for your perusal</h2>
		<a href="/single" class="btn variant-filled-primary shadow-lg text-2xl">
			<Icon src={hi.ArrowTopRightOnSquare} size="22" />
			<span class="mr-0.5">Search</span>
		</a>
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
