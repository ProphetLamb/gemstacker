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
	const { description, title } = !!gem ? getRecipeInfo(gem.preferred_recipe) : {};
	$: preferredRecipie = gem?.recipes[gem.preferred_recipe];
</script>

<div class="card p-2 variant-soft-surface space-y-2 {$$props.class ?? ''}">
	<div class="flex flex-row justify-between">
		<h2>{gem?.name ?? ''}</h2>

		{#if !!close}
			<div>
				<button
					type="button"
					class="button variant-ghost rounded-full w-6 h-6 flex justify-center items-center"
					on:click={close}><Icon src={hi.XMark} size="18" /></button
				>
			</div>
		{/if}
	</div>
	<p>{preferredRecipie?.buy.listing_count ?? 0} available</p>
	<h3>Best Recipe: {title}</h3>
	<p>{@html description?.replaceAll('\n', '<br/>') ?? ''}</p>
	<h3>All Recipes</h3>
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

<style lang="postcss">
	hr:last-child {
		@apply hidden;
	}
</style>
