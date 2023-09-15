<script lang="ts">
	import { superForm } from 'sveltekit-superforms/client';
	import type { ActionData, PageData } from './$types';
	import { gemProfitRequestParameterSchema } from '$lib/gemLevelProfitApi';
	import GemTradeQueryButton from '$lib/client/GemTradeQueryButton.svelte';

	export let data: PageData;
	export let form: ActionData;
	const {
		form: gemLevelsProfitForm,
		errors,
		constraints,
		enhance,
		delayed
	} = superForm(data.gemLevelsProfitForm, {
		validators: gemProfitRequestParameterSchema
	});
</script>

<div class="container h-full mx-auto flex flex-wrap gap-8 justify-center items-center">
	<div class="space-y-10 text-center flex flex-col items-center">
		<h2 class="h2">Gem levels for profit.</h2>
		<form class="space-y-2" use:enhance method="POST" action="?/getGemLevelProfit">
			<label class="label">
				<span>Gem Name</span>
				<input
					name="gem_name"
					class="input"
					type="text"
					bind:value={$gemLevelsProfitForm.gem_name}
					{...$constraints.gem_name}
				/>
				{#if $errors.gem_name}
					<aside class="alert variant-glass-error">{$errors.gem_name}</aside>
				{/if}
			</label>
			<label class="label">
				<span>Sell price (minimum Chaos value)</span>
				<input
					name="min_sell_price_chaos"
					class="input"
					type="number"
					bind:value={$gemLevelsProfitForm.min_sell_price_chaos}
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
					bind:value={$gemLevelsProfitForm.max_buy_price_chaos}
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
					min={1000000}
					max={100000000}
					step={1000000}
					bind:value={$gemLevelsProfitForm.min_experience_delta}
					{...$constraints.min_experience_delta}
				/>
				<p>{$gemLevelsProfitForm.min_experience_delta}exp</p>
				{#if $errors.min_experience_delta}
					<aside class="alert variant-glass-error">{$errors.min_experience_delta}</aside>
				{/if}
			</label>
			<button class="btn variant-filled" type="submit"> Search </button>
		</form>
	</div>
	<div class="text-token flex flex-col items-center card p-4 space-y-4 w-[46rem]">
		<h3 class="h3">The best gems for you.</h3>
		{#if $delayed}
			Loading..
		{:else if form?.gemProfit}
			<ol class="list w-full">
				{#each Object.entries(form.gemProfit.data) as [name, data], idx}
					<li>
						<span class="badge-icon p-4 variant-soft-secondary">{idx + 1}.</span>
						<span class="flex-auto">{name}</span>
						<span>{data.min.price}c @ lvl{data.min.level}</span>
						<span>{data.max.price}c @ lvl{data.max.level}</span>
						<GemTradeQueryButton {data} {name} />
					</li>
				{/each}
			</ol>
		{:else}
			<p>Enter your search criteria above.</p>
		{/if}
	</div>
</div>
