<script lang="ts">
	import { intlCompactNumber, intlFractionNumber } from '$lib/intl';
	import { currencyRerollRare } from '$lib/knownImages';
	import type { LoadoutResponse } from '$lib/loadout';

	export let loadout: LoadoutResponse;
	$: totalBuyCost = loadout.totalBuyCost;
	$: totalExperience = loadout.totalExperience;
	$: totalSellPrice = loadout.totalSellPrice;
	$: count = loadout.count;
</script>

Buy <span class="font-semibold text-token mx-1">{count}</span>gems for
<span class="font-semibold text-token ml-1">{intlFractionNumber.format(totalBuyCost)}</span>
<img src={currencyRerollRare} alt="c" class="h-4 w-4 mt-1" />, earn
<span class="font-semibold text-secondary-300-600-token mx-1"
	>+{intlCompactNumber.format(totalExperience)}</span
>exp, sell for
<span class="font-semibold text-token ml-1">{intlFractionNumber.format(totalSellPrice)}</span
><img src={currencyRerollRare} alt="c" class="h-4 w-4 mt-1" />
