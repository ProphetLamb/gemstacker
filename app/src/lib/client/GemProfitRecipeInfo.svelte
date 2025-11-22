<script lang="ts">
	import Chaos from './Chaos.svelte';
	import { intlCompactNumber, intlFixed2Number, intlFixed4Number } from '$lib/intl';
	import { currencyTypeDisplay, type GemProfitResponseItem } from '$lib/gemLevelProfitApi';
	import { getRecipeInfo, wellKnownProbabilisticLabelDisplay } from '$lib/recipe';
	import { Icon } from '@steeze-ui/svelte-icon';
	import * as hi from '@steeze-ui/heroicons';
	import type { MouseEventHandler } from 'svelte/elements';
	import Currency from './Currency.svelte';
	import RecipeInfoProbabilities from './RecipeInfoProbabilities.svelte';
	import RecipeInfoAllRecipes from './RecipeInfoAllRecipes.svelte';
	import RecipeInfoRecipeCost from './RecipeInfoRecipeCost.svelte';

	export let gem: GemProfitResponseItem | undefined | null;
	export let close: MouseEventHandler<HTMLButtonElement> | undefined | null = undefined;
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
			<span>Buy lvl{preferredRecipe?.buy.level ?? 0}/{preferredRecipe?.buy.quality ?? 0}q for</span>
			<span class="inline-block"> <Chaos value={preferredRecipe?.buy.price ?? 0} /></span>
			<span class="whitespace-nowrap">{preferredRecipe?.buy.listing_count ?? 0} available</span>
			<span class="text-error-400-500-token {gem?.min.corrupted ? 'inline' : 'hidden'}"
				>corrupted</span
			>
		</p>
		<RecipeInfoAllRecipes recipes={gem?.recipes} />
	</div>
</div>
