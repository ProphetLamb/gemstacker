<script lang="ts">
	import GemTradeQueryButton from '$lib/client/GemTradeQueryButton.svelte';
	import { wellKnownExchangeRateDisplay, type GemProfitResponseItem } from '$lib/gemLevelProfitApi';
	import GemTableIdentifier from '$lib/client/GemTableIdentifier.svelte';
	import { inspectProfit } from '$lib/client/gemProfitRecipeInfo';
	import { onDestroy } from 'svelte';
	import { gainMarginToTextColor, getRecipeInfo } from '$lib/recipe';
	import { Icon } from '@steeze-ui/svelte-icon';
	import * as hi from '@steeze-ui/heroicons';
	import { intlFixed4Number } from '$lib/intl';
	import CurrencyIcon from './CurrencyIcon.svelte';

	export let data: GemProfitResponseItem[];
	onDestroy(() => {
		$inspectProfit.gem = undefined;
	});
</script>

<table class="list w-full border-separate border-spacing-y-2 {$$props.class ?? ''}">
	<thead class="sticky top-0 p-4 z-[1]">
		<tr
			class="align-center text-center text-sm rounded-b-md bg-surface-100/75 dark:bg-surface-800/90"
		>
			<th title="Icon and link to poebw.tw"><span>Icon</span></th>
			<th title="Gem name and experience required to level the gem" class="text-left">Gem Level</th>
			<th title="Purchase the gem for this price, see Recipe Info for specifics.">Buy</th>
			<th title="Apply currency to the gem, see Recipe Info for specifics.">Apply</th>
			<th />
			<th title="Average profit per gem for the recipe, see Recipe Info for specifics.">Profit</th>
			<th title="The average profit in chaos per 1M exp, see Recipe Info for specifics." class="max-md:hidden">
				<div class="flex flex-row items-center">
					<CurrencyIcon {...wellKnownExchangeRateDisplay.chaos_orb} />
					<span>&#47;</span>
					<span>1M</span>
					<span>exp</span>
				</div>
			</th>
			<th title="Click on Buy or Sell to check the price on PoE trade" class="max-sm:hidden"
				>Trade</th
			>
		</tr>
	</thead>
	<tbody>
		{#each data ?? [] as gem, idx}
			{@const recipe = getRecipeInfo(gem.preferred_recipe)}
			<tr
				class="h-12 hover:brightness-110"
				on:mouseover={() => ($inspectProfit.gem = gem)}
				on:focus={() => ($inspectProfit.gem = gem)}
				title={recipe.description}
			>
				<GemTableIdentifier {gem} {idx} />
				<td class="max-md:hidden text-center">
					<span>≃</span>
					<span class="{gainMarginToTextColor(gem.gain_margin)}">{intlFixed4Number.format(gem.gain_margin)}</span>
				</td>
				<td class="pl-2 max-sm:hidden">
					<GemTradeQueryButton gemPrice={gem} />
				</td>
			</tr>
		{/each}
	</tbody>
</table>
