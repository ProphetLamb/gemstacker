<script lang="ts">
	import { Icon } from '@steeze-ui/svelte-icon';
	import * as hi from '@steeze-ui/heroicons';
	import { wellKnownExchangeRateDisplay, type GemProfitResponseItem } from '$lib/gemLevelProfitApi';
	import { intlCompactNumber } from '$lib/intl';
	import Chaos from './Chaos.svelte';
	import Currency from './Currency.svelte';
	import { profitToTextColor } from '$lib/recipe';

	export let gem: GemProfitResponseItem;
	export let idx: number;
	const { adjusted_earnings, experience_delta, recipe_cost, probabilistic, gain_margin } = gem.recipes[
		gem.preferred_recipe
	] ?? { adjusted_earnings: 0, experience_delta: 0 };
</script>

<td class="">
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
	<div class="flex flex-row items-center justify-end">
		<Chaos value={gem.min.price} />
	</div>
</td>
<td>
	<div class="flex flex-wrap justify-center">
		{#each Object.values(wellKnownExchangeRateDisplay) as display}
			{@const quantity = (recipe_cost ?? {})[display.title]}
			{#if !!quantity}
				<Currency
					value={-quantity}
					value_class="hidden text-warning-400-500-token"
					{...display}
				/>
			{/if}
		{/each}
	</div>
</td>
<td class="w-fit text-surface-600-300-token">
	<Icon src={hi.ArrowRight} size="14" />
</td>
<td>
	<div class="flex flex-row justify-end md:h-full items-center">
		<Chaos
			value_prefix={adjusted_earnings > 0 ? '+' : ''}
			value={adjusted_earnings}
			value_class={profitToTextColor(adjusted_earnings)}
		/>
	</div>
</td>