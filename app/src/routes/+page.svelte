<script lang="ts">
	import { ProgressRadial } from '@skeletonlabs/skeleton';
	import AnimatedSearchButton from './../lib/client/AnimatedSearchButton.svelte';
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

<div class="w-full h-full mx-auto flex flex-wrap gap-4 justify-center items-center">
	<article class="space-y-10 text-center flex flex-col items-center p-4">
		<h1 class="h1">
			<span class="text-shadow shadow-surface-500"> Gem levels for </span><span
				class="bg-clip-text shadow-surface-500 text-transparent bg-gradient-to-tr from-indigo-500 to-sky-300 via-accent animate-gradient-xy font-bold"
				>profit</span
			>.
		</h1>
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
			</label><button
				type="submit"
				class="shadow-lg btn text-token variant-filled bg-gradient-to-br from-indigo-700 to-fuchsia-800 shadow-fuchsia-900/50 after:bg-gradient-to-br after:from-orange-800 after:to-amber-400 hover:shadow-orange-400/50 via-accent animate-gradient-x after:animate-gradient-x text-2xl transition-all duration-[1s]"
			>
				Search</button
			>
		</form>
	</article>
	<article class="flex flex-col items-center p-4">
		<h1 class="h1 flex flex-row items-center space-x-4 pb-4 text-shadow shadow-surface-500">
			<Icon src={hi.Sparkles} theme="solid" class=" text-yellow-300" size="32" />
			<span class="">The best gems for you.</span>
		</h1>
		<div class="text-token flex flex-col items-center card p-4 space-y-2 w-[46rem]">
			{#if $delayed}
				<div class="flex flex-row items-center">
					<ProgressRadial stroke={10} value={undefined} class="w-4" />
					<span class="ml-2 text-center">Loading...</span>
				</div>
			{:else if form?.gemProfit}
				<table class="list w-full border-separate border-spacing-y-2 border-spacing-x-1">
					<tbody>
						{#each form.gemProfit as gemPrice, idx}
							<tr class="h-12">
								<td class="pr-2">
									<div class="badge-icon variant-soft-primary h-11 w-11">
										<img src={``} alt={`${idx + 1}`} />
									</div>
								</td>
								<td class="flex-auto">{gemPrice.name}</td>
								<td class="align-middle text-right">{gemPrice.min.price}c</td>
								<td class="align-middle text-center"><Icon src={hi.AtSymbol} size="16" /></td>
								<td class="align-middle text-left">lvl{gemPrice.min.level}</td>
								<td class="align-middle"><Icon src={hi.ArrowRight} size="16" /></td>
								<td class="align-middle text-right">{gemPrice.max.price}c</td>
								<td class="align-middle text-center"><Icon src={hi.AtSymbol} size="16" /></td>
								<td class="align-middle text-left">lvl{gemPrice.max.level}</td>
								<td class="pl-2">
									<GemTradeQueryButton {gemPrice} />
								</td>
							</tr>
						{/each}
					</tbody>
				</table>
			{:else}
				<p>Enter your criteria or just <span class="font-extrabold">search</span></p>
			{/if}
		</div>
	</article>
</div>

<style lang="postcss">
	span.bg-clip-text {
		filter: drop-shadow(0 1px 2px var(--tw-shadow-color));
	}

	button[type='submit'] {
		animation-duration: 2s;
		position: relative;
		overflow: hidden;
		z-index: 0;
		&:after {
			animation-duration: 2s;
			clip-path: path(
				'M0,137.087 L300,137.087 L300,8.754 C300,8.754 283.833,13.254 271.833,13.254 C244.833,13.254 224.167,0 192.5,0 C153,0 128.167,14.587 100.5,14.587 C80.167,14.587 64.5,1.337 41.75,1.337 C25,1.337 12,4.92 0,9.087 L0,137.087 Z'
			);
			content: '';
			position: absolute;
			top: -50%;
			left: 0;
			height: 150%;
			width: 200%;
			transform: translate(-50%, 100%);
			transform-origin: top;
			transition: 1s transform ease;
			will-change: transform;
			z-index: -1;
		}
		&:hover::after {
			transform: translate(0, 0);
		}
	}
</style>
