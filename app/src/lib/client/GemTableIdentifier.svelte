<script lang="ts">
	import { Icon } from '@steeze-ui/svelte-icon';
	import * as hi from '@steeze-ui/heroicons';
	import { wellKnownExchangeRateDisplay, type GemProfitResponseItem } from '$lib/gemLevelProfitApi';
	import { intlCompactNumber } from '$lib/intl';
	import Chaos from './Chaos.svelte';
	import Currency from './Currency.svelte';
	import { getRecipeInfo } from '$lib/recipe';

	export let gem: GemProfitResponseItem;
	export let idx: number;
	const { adjusted_earnings, experience_delta, recipe_cost, probabilistic } = gem.recipes[
		gem.preferred_recipe
	] ?? { adjusted_earnings: 0, experience_delta: 0 };
</script>

<td class="pr-2">
	<a
		href={gem.foreign_info_url}
		target="_blank"
		title="Open poedb.tw"
		class="badge-icon h-11 w-11 {gem.max.corrupted ? 'variant-soft-error' : 'variant-soft-primary'}"
	>
		<img src={gem.icon} alt={`${idx + 1}`} />
	</a>
</td>
<td class="border-spacing-0 text-start">
	<div class="flex flex-col h-full" >
		<span class="align-top">{gem.name}</span>
		<div class=" text-xs text-surface-600-300-token">
			lvl
			{gem.min.level} → {gem.max.level}
			{probabilistic ? '≃' : '='}
			<span class="text-secondary-500-400-token"
				>+{intlCompactNumber.format(experience_delta ?? 0)}</span
			>exp
		</div>
	</div>
</td>
<td>
	<div class="md:flex md:flex-row md:h-full items-center justify-end">
		<Chaos value={gem.min.price} />
	</div>
</td>
<td class="w-fit text-surface-600-300-token">
	<Icon src={hi.ArrowRight} size="14" />
</td>
<td>
	<div class="md:flex md:flex-row md:h-full items-center justify-end">
		<Chaos value={gem.max.price} />
	</div>
</td>
<td>
	<div class="flex flex-wrap justify-end">
		{#each Object.values(wellKnownExchangeRateDisplay) as display}
			{@const quantity = (recipe_cost ?? {})[display.name]}
			{#if !!quantity}
				<Currency
					value={-quantity}
					value_class="text-warning-300-600-token"
					src={display.img}
					alt={display.alt}
				/>
			{/if}
		{/each}
	</div>
</td>
<td> <span class="font-semibold text-surface-600-300-token">{probabilistic ? '≃' : '='}</span></td>
<td>
	<div class="flex justify-end">
		<div class="md:flex md:flex-row md:h-full items-center">
			<Chaos
				value={adjusted_earnings}
				value_class={adjusted_earnings >= 0
					? 'text-success-200-700-token'
					: 'text-error-200-700-token'}
			/>
		</div>
	</div>
</td>
