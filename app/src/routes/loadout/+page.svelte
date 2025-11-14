<script lang="ts">
	import BetterTrading from '$lib/client/BetterTrading.svelte';
	import GemFilter from '$lib/client/GemFilter.svelte';
	import type { ActionData, PageData } from './$types';
	import AnimatedSearchButton from '$lib/client/AnimatedSearchButton.svelte';
	import * as hi from '@steeze-ui/heroicons';
	import { Icon } from '@steeze-ui/svelte-icon';
	import { Wrapper, WrapperItem } from '$lib/client/wrapper';
	import { localSettings } from '$lib/client/localSettings';
	import { superForm } from 'sveltekit-superforms/client';
	import { LoadoutOptimizer, loadoutRequestSchema, maxCountLoadout } from '$lib/loadout';
	import LoadingPlaceholder from '$lib/client/LoadingPlaceholder.svelte';
	import { ProgressRadial } from '@skeletonlabs/skeleton';
	import LoadoutTable from '$lib/client/LoadoutTable.svelte';
	import LoadoutInfo from '$lib/client/LoadoutInfo.svelte';
	import { availableGems } from '$lib/client/availableGems';
	import { getStateFromQuery, replaceStateWithQuery } from '$lib/client/navigation';
	import { browser } from '$app/environment';
	import { onMount } from 'svelte';
	import LocalSettings from '$lib/client/LocalSettings.svelte';

	export let data: PageData;
	export let form: ActionData;

	const {
		form: loadoutForm,
		capture,
		restore,
		errors,
		constraints,
		enhance,
		delayed
	} = superForm(data.loadoutForm, {
		validators: loadoutRequestSchema,
		taintedMessage: null
	});

	export const snapshot = { capture, restore };

	$: $availableGems = form?.gemProfit;
	$: excludedGems = new Set($localSettings.exclude_gems);
	$: loadout =
		$delayed || !$availableGems
			? undefined
			: new LoadoutOptimizer(
					$loadoutForm,
					!excludedGems
						? $availableGems
						: $availableGems.filter((x) => !excludedGems.has(x.name.toLowerCase()))
			  ).optimize();

	let htmlLoadoutForm: HTMLFormElement;

	function submitWhenSettingsChanged(e: CustomEvent<any>) {
		if (e.detail.changed) {
			htmlLoadoutForm.requestSubmit();
		}
	}

	function fillFromFromQuery() {
		function num(s?: string): number | undefined {
			try {
				return !!s ? parseInt(s) : undefined
			} catch {
				return undefined
			}
		}
		// fill form from query
		$loadoutForm = {
			...$loadoutForm,
			...getStateFromQuery((x) => {
				return {
					red: num(x['red']),
					green: num(x['green']),
					blue: num(x['blue']),
					white: num(x['white'])
				};
			})
		};
	}

	function shouldAutoRequestForm() {
		return !$availableGems && maxCountLoadout($loadoutForm) > 0
	}

	function submitFormFilledFromQuery() {
		// remove query parameters
		replaceStateWithQuery({
			red: undefined,
			green: undefined,
			blue: undefined,
			white: undefined
		});
		// submit form filled via query
		htmlLoadoutForm.requestSubmit();
	}

	if (browser) {
		fillFromFromQuery();
	}
	onMount(async () => {
		if (browser && shouldAutoRequestForm()) {
			submitFormFilledFromQuery();
		}
	});
</script>

<Wrapper>
	<WrapperItem>
		<h1 class="h1">
			Your <span
				class="bg-clip-text shadow-surface-500 text-transparent bg-gradient-to-tr from-indigo-500 to-sky-300 via-accent animate-gradient-xy font-bold"
				>loadout</span
			>, your gems.
		</h1>
		<form class="space-y-2" bind:this={htmlLoadoutForm} use:enhance method="POST">
			<input type="hidden" name="league" value={$localSettings.league} />
			<input
				type="hidden"
				name="min_experience_delta"
				value={$localSettings.min_experience_delta}
			/>
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
		<div class="text-token flex flex-col items-center card p-4 space-y-2">
			{#if $delayed}
				<LoadingPlaceholder
					class="w-[55rem] max-w-[calc(100vw-4rem)]"
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
			{:else if loadout && loadout.items.length > 0}
				<LoadoutInfo bind:loadout>
					<GemFilter />
					<LocalSettings on:close={submitWhenSettingsChanged} />
				</LoadoutInfo>
				<LoadoutTable bind:data={loadout.items} />
				<BetterTrading data={loadout.items.map((x) => x.gem)} />
			{:else}
				<LoadoutInfo
					loadout={{ count: 0, items: [], totalBuyCost: 0, totalExperience: 0, totalSellPrice: 0 }}
				>
					{#if $availableGems && $availableGems.length > 0}
						<GemFilter />
					{/if}
					<LocalSettings on:close={submitWhenSettingsChanged} />
				</LoadoutInfo>
				<LoadingPlaceholder
					class="w-[55rem] max-w-[calc(100vw-4rem)]"
					front="bg-surface-backdrop-token"
					rows={10}
				>
					<p class="text-xl">
						Enter your criteria or just <span class="font-extrabold">search</span>
					</p>
				</LoadingPlaceholder>
			{/if}
		</div>
	</WrapperItem>
</Wrapper>

<style lang="postcss"></style>
