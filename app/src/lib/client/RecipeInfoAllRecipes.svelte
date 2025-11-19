<script lang="ts">
	import type {
		GemProfitResponeItemMargin,
		GemProfitResponseItemRecipeName
	} from '$lib/gemLevelProfitApi';
	import { intlCompactNumber, intlFixed4Number } from '$lib/intl';
	import { getRecipeInfo } from '$lib/recipe';
	import { Accordion, AccordionItem } from '@skeletonlabs/skeleton';
	import Chaos from './Chaos.svelte';
	import RecipeInfoProbabilities from './RecipeInfoProbabilities.svelte';
	import RecipeInfoRecipeCost from './RecipeInfoRecipeCost.svelte';
	export let recipes:
		| Partial<Record<GemProfitResponseItemRecipeName, GemProfitResponeItemMargin>>
		| undefined
		| null;

	$: sortedRecipes = Object.entries(recipes ?? {}).toSorted(
		(x, y) => y[1].gain_margin - x[1].gain_margin
	);

	function accordionBgColor(gain: GemProfitResponeItemMargin) {
		return gain.gain_margin > 0 ? 'bg-lime-500/5' : 'bg-red-500/5';
	}
</script>

<Accordion>
	{#each sortedRecipes as [name, recipe], idx}
		{@const recipeInfo = getRecipeInfo(name)}
		<AccordionItem
			autocollapse
			open={idx === 0}
			class={idx === 0 ? 'bg-lime-500/10' : accordionBgColor(recipe)}
		>
			<svelte:fragment slot="summary">
				<div class="flex flex-col items-start align-middle w-full">
					<span class="mr-1">{recipeInfo.title} - {recipe.sell.listing_count} selling</span>
					<div class="ml-auto flex flex-row items-end text-sm">
						<Chaos value={recipe.adjusted_earnings} />
						<span>&#47;{intlCompactNumber.format(recipe.experience_delta)}exp</span>
						<span>={intlFixed4Number.format(recipe.gain_margin)}&permil;</span>
					</div>
				</div>
			</svelte:fragment>
			<svelte:fragment slot="content">
				<h5 class="h5">{recipeInfo.title}</h5>
				<p>{@html recipeInfo.description?.replaceAll('\n', '<br/>') ?? ''}</p>
				{#if recipe?.recipe_cost}
					<h5 class="h5">Recipe Cost</h5>
					<RecipeInfoRecipeCost {recipe} />
				{/if}
				{#if recipe.probabilistic}
					<h5 class="h5">Probabilities</h5>
					<RecipeInfoProbabilities probabilities={recipe.probabilistic} />
				{/if}
			</svelte:fragment>
		</AccordionItem>
	{/each}
</Accordion>
