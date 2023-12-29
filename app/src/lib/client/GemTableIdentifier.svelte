<script lang="ts">
	import { Icon } from '@steeze-ui/svelte-icon';
	import * as hi from '@steeze-ui/heroicons';
	import type { GemProfitResponseItem } from '$lib/gemLevelProfitApi';
	import { currencyGemQuality, currencyRerollRare } from '$lib/knownImages';
	import { intlCompactNumber, intlFractionNumber } from '$lib/intl';

	export let gem: GemProfitResponseItem;
	export let idx: number;

	let deltaExp = gem.max.experience - gem.min.experience;
	let deltaQty = Math.max(0, gem.max.quality - gem.min.quality);
	let deltaPrice = Math.max(0, gem.max.price - gem.min.price - deltaQty);
</script>

<td class="pr-2">
	<a href={gem.foreign_info_url} target="_blank" class="badge-icon variant-soft-primary h-11 w-11">
		<img src={gem.icon} alt={`${idx + 1}`} />
	</a>
</td>
<td class="border-spacing-0 text-start">
	<a href={gem.foreign_info_url} target="_blank" class="flex flex-col h-full"
		><span class="align-top">{gem.name}</span>
		<div class=" text-xs text-surface-600-300-token">
			lvl
			{gem.min.level} → {gem.max.level} =
			<span class="text-secondary-300-600-token">+{intlCompactNumber.format(deltaExp)}</span>exp
		</div></a
	>
</td>
<td />
<td class="text-end pt-1">
	<span>{gem.min.price}</span>
	<img src={currencyRerollRare} alt="c" class="table-cell h-4 w-4" />
</td>
<td class="pt-1 text-surface-600-300-token">
	<Icon src={hi.ArrowRight} size="14" />
</td>
<td class="text-start pt-1">
	<span class="whitespace-nowrap">
		{gem.max.price}<img src={currencyRerollRare} alt="c" class="table-cell h-4 w-4" />
	</span>
	{#if deltaQty > 0}
		<span class="whitespace-nowrap">
			<span class="text-error-200-700-token">-{deltaQty}</span><img
				src={currencyGemQuality}
				alt="qty"
				class="table-cell h-4 w-4"
			/>
		</span>
	{/if}
</td>
<td> <span class="font-semibold text-surface-600-300-token">=</span></td>
<td class="text-end pt-1">
	<span class="text-success-200-700-token">+{intlFractionNumber.format(deltaPrice)}</span><img
		src={currencyRerollRare}
		alt="c"
		class="table-cell h-4 w-4"
	/>
</td>
