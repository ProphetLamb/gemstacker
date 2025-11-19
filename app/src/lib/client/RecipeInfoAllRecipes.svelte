<script lang="ts">
	import type { GemProfitResponeItemMargin, GemProfitResponseItemRecipeName } from '$lib/gemLevelProfitApi';
	import { intlCompactNumber, intlFixed4Number } from '$lib/intl';
	import { getRecipeInfo } from '$lib/recipe';
	import Chaos from './Chaos.svelte';
	export let recipes: Partial<Record<GemProfitResponseItemRecipeName, GemProfitResponeItemMargin>> | undefined | null;
</script>

<ul>
	{#each Object.entries(recipes ?? {}) as [recipe, gain]}
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
