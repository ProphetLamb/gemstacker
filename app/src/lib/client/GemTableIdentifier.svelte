<script lang="ts">
	import { Icon } from '@steeze-ui/svelte-icon';
	import * as hi from '@steeze-ui/heroicons';
	import type { GemProfitResponseItem } from '$lib/gemLevelProfitApi';
	import { currencyGemQuality, currencyRerollRare } from '$lib/knownImages';
	import { intlCompactNumber, intlFractionNumber } from '$lib/intl';
	import Chaos from './Chaos.svelte';
	import Currency from './Currency.svelte';

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
<td>
	<div class="md:flex md:flex-row md:h-full items-center justify-end">
		<Chaos value={gem.min.price} />
	</div>
</td>
<td class="text-surface-600-300-token">
	<Icon src={hi.ArrowRight} size="14" />
</td>
<td>
	<div class="md:flex md:flex-row md:h-full items-center justify-end">
		<Chaos value={gem.max.price} />
	</div>
</td>
<td>
	{#if deltaQty > 0}
		<Currency
			value={-deltaQty}
			value_class="text-error-400-500-token"
			src={currencyGemQuality}
			alt="gcp"
		/>
	{/if}
</td>
<td> <span class="font-semibold text-surface-600-300-token">=</span></td>
<td>
	<div class="md:flex md:flex-row md:h-full items-center justify-end">
		<Chaos value={deltaPrice} />
	</div>
</td>
