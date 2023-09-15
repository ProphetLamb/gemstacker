<script lang="ts">
	import { superForm } from 'sveltekit-superforms/client';
	import type { ActionData, PageData } from './$types';
	import { gemProfitRequestParameterSchema } from '$lib/gemLevelProfitApi';
	import GemTradeQueryButton from '$lib/client/GemTradeQueryButton.svelte';
	import { Icon } from '@steeze-ui/svelte-icon';
	import * as hi from '@steeze-ui/heroicons';

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
		<h2 class="h2">
			Gem levels for <span
				class="bg-clip-text text-transparent bg-gradient-to-tr from-primary-500 to-tertiary-300 via-accent animate-gradient-xy font-extrabold"
				>profit</span
			>.
		</h2>
		<form class="space-y-2" use:enhance method="POST" action="?/getGemLevelProfit">
			<label class="label">
				<span>Gem Name</span>
				<input
					name="gem_name"
					class="input"
					type="text"
					placeholder="Gem name glob..."
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
					placeholder="Minimum sell price..."
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
					placeholder="Maximum buy price..."
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
			<button
				class="shadow-lg btn variant-filled bg-gradient-to-br hover:from-orange-600 hover:to-amber-200 hover:shadow-orange-400/50 from-indigo-700 to-fuchsia-800 shadow-indigo-500/50 via-accent animate-gradient-x text-2xl"
				type="submit"
			>
				Search
			</button>
		</form>
	</div>
	<div class="flex flex-col items-center">
		<h2 class="h2 flex flex-row items-center space-x-4 pb-4">
			<Icon src={hi.Sparkles} theme="solid" class=" text-yellow-300" size="32" />
			<span>The best gems for you.</span>
		</h2>
		<div class="text-token flex flex-col items-center card p-4 space-y-2 w-[46rem]">
			{#if $delayed}
				Loading..
			{:else if form?.gemProfit}
				<table class="list w-full">
					<tbody>
						{#each Object.entries(form.gemProfit.data) as [name, data], idx}
							<tr class="h-12">
								<td>
									<div class="badge-icon variant-soft-primary h-11 w-11">
										<img src={``} alt={`${idx + 1}`} />
									</div>
								</td>
								<td class="flex-auto">{name}</td>
								<td class="align-middle text-right">{data.min.price}c</td>
								<td class="align-middle text-center"><Icon src={hi.AtSymbol} size="16" /></td>
								<td class="align-middle text-left">lvl{data.min.level}</td>
								<td class="align-middle"><Icon src={hi.ArrowRight} size="16" /></td>
								<td class="align-middle text-right">{data.max.price}c</td>
								<td class="align-middle text-center"><Icon src={hi.AtSymbol} size="16" /></td>
								<td class="align-middle text-left">lvl{data.max.level}</td>
								<td>
									<GemTradeQueryButton {data} {name} />
								</td>
							</tr>
						{/each}
					</tbody>
				</table>
			{:else}
				<p>Enter your criteria or just <span class="font-extrabold">search</span></p>
			{/if}
		</div>
	</div>
</div>
