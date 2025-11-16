<script lang="ts">
	import Chaos from './Chaos.svelte';
	import { intlCompactNumber, intlFixed4Number } from '$lib/intl';
	import type { GemProfitResponseItem } from '$lib/gemLevelProfitApi';
	import { getRecipeInfo } from '$lib/recipe';
	import { Icon } from '@steeze-ui/svelte-icon';
	import * as hi from '@steeze-ui/heroicons';
	import type { MouseEventHandler } from 'svelte/elements';

	export let gem: GemProfitResponseItem | undefined | null;
	export let close: MouseEventHandler<HTMLButtonElement> | undefined | null = undefined;
	$: info = getRecipeInfo(gem?.preferred_recipe);
	$: preferredRecipie = gem?.recipes[gem.preferred_recipe];
</script>

<div class="card p-2 flex flex-col {$$props.class ?? ''}">
	<div class="flex flex-row justify-between">
		<h4 class="h4 font-bold">{gem?.name ?? ''}</h4>

		{#if !!close}
			<div>
				<button
					type="button"
					class="btn align-middle variant-ghost p-0 w-8 h-8"
					on:click={close}><Icon src={hi.XMark} size="18" /></button
				>
			</div>
		{/if}
	</div>
	<div class="space-y-4">
		<p>{preferredRecipie?.buy.listing_count ?? 0} available</p>
		<h5 class="h5">Best Recipe: {info.title}</h5>
		<p>{@html info.description?.replaceAll('\n', '<br/>') ?? ''}</p>
		<h5 class="h5">All Recipes</h5>
		<ul>
			{#each Object.entries(gem?.recipes ?? {}) as [recipe, gain], idx}
				{@const recipieInfo = getRecipeInfo(recipe)}
				<li class="flex flex-col items-start align-middle">
					<span class="mr-1">{recipieInfo.title} - {gain.sell.listing_count} selling</span>
					<div class="flex flex-row items-end">
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
</style>
