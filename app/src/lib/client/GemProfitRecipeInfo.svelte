<script lang="ts">
	import Chaos from './Chaos.svelte';
	import { intlCompactNumber, intlFixed2Number, intlFixed4Number } from '$lib/intl';
	import { currencyTypeDisplay, type GemProfitResponseItem } from '$lib/gemLevelProfitApi';
	import { getRecipeInfo, wellKnownProbabilisticLabelDisplay } from '$lib/recipe';
	import { Icon } from '@steeze-ui/svelte-icon';
	import * as hi from '@steeze-ui/heroicons';
	import type { MouseEventHandler } from 'svelte/elements';
	import Currency from './Currency.svelte';

	export let gem: GemProfitResponseItem | undefined | null;
	export let close: MouseEventHandler<HTMLButtonElement> | undefined | null = undefined;
	$: info = getRecipeInfo(gem?.preferred_recipe);
	$: preferredRecipe = gem?.recipes[gem.preferred_recipe];
</script>

<div class="card p-2 flex flex-col {$$props.class ?? ''}">
	<div class="flex flex-row justify-between">
		<h4 class="h4 font-bold">{gem?.name ?? ''}</h4>

		{#if !!close}
			<div>
				<button type="button" class="btn-icon variant-ghost w-8 h-8" on:click={close}
					><Icon src={hi.XMark} size="18" /></button
				>
			</div>
		{/if}
	</div>
	<div>
		<p>
			{preferredRecipe?.buy.listing_count ?? 0} available
			<span class="text-error-400-500-token {gem?.min.corrupted ? 'inline' : 'hidden'}"
				>corrupted</span
			>
		</p>
		<h5 class="h5">{info.title}</h5>
		<p>{@html info.description?.replaceAll('\n', '<br/>') ?? ''}</p>
		{#if preferredRecipe?.recipe_cost}
			<h5 class="h5">Recipe Cost</h5>
			<ul>
				{#each Object.entries(preferredRecipe.recipe_cost) as [key, cost]}
					{@const currency = currencyTypeDisplay(key)}
					<li class="flex flex-row justify-between">
						<span>{currency.name}</span>
						<Currency value={cost} alt={currency.alt} src={currency.img} />
					</li>
					<hr />
				{/each}
			</ul>
			{#if preferredRecipe?.min_attempts_to_profit}
				<p
					class="{preferredRecipe.min_attempts_to_profit < 3
						? 'text-success-400-500-token'
						: 'text-warning-400-500-token'} font-semibold"
				>
				{#if preferredRecipe.min_attempts_to_profit === 1}
					Guaranteed profit with this recipe.
				{:else}
					At least {preferredRecipe.min_attempts_to_profit} attempts needed for a 66% expectation of significant profit.
				{/if}
				</p>
			{/if}

			{#if preferredRecipe?.min_attempts_to_profit === 0}
				<p class="text-error-400-500-token font-semibold">
					No profit expectation at all with this recipe.
				</p>
			{/if}
		{/if}
		{#if preferredRecipe?.probabilistic}
			<h5 class="h5">Probabilities</h5>
			<ul>
				{#each preferredRecipe.probabilistic as prob}
					{@const label = wellKnownProbabilisticLabelDisplay[prob.label ?? 'misc']}
					<li class="flex flex-col items-start align-middle w-full">
						<span>{label}</span>
						<div class="ml-auto flex flex-row items-end text-sm">
							<Chaos value={prob.earnings} />
							<span>@{intlFixed2Number.format(prob.chance * 100)}%</span>
						</div>
					</li>
					<hr />
				{/each}
			</ul>
		{/if}
		<h5 class="h5">All Recipes</h5>
		<ul>
			{#each Object.entries(gem?.recipes ?? {}) as [recipe, gain]}
				{@const recipeInfo = getRecipeInfo(recipe)}
				<li class="flex flex-col items-start align-middle w-full">
					<span class="mr-1">{recipeInfo.title} - {gain.sell.listing_count} selling</span>
					<div class="ml-auto flex flex-row items-end text-sm">
						<Chaos value={gain.adjusted_earnings} />
						<span>&#47;{intlCompactNumber.format(gain.experience_delta)}exp</span>
						<span>={intlFixed4Number.format(gain.gain_margin)}&permil;</span>
					</div>
				</li>
				<hr />
			{/each}
		</ul>
	</div>
</div>

<style lang="postcss">
	hr:last-child {
		@apply hidden;
	}
	h5 {
		@apply mt-4;
	}
</style>
