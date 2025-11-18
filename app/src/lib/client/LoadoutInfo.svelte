<script lang="ts">
	import { wellKnownExchangeRateDisplay } from '$lib/gemLevelProfitApi';
	import { intlCompactNumber, intlFractionNumber } from '$lib/intl';
	import type { LoadoutResponse } from '$lib/loadout';

	export let loadout: LoadoutResponse;
	$: totalBuyCost = loadout.totalBuyCost;
	$: totalExperience = loadout.totalExperience;
	$: totalSellPrice = loadout.totalSellPrice;
	$: count = loadout.count;
</script>

Buy <span class="font-semibold text-token mx-1">{count}</span>gems for
<span class="font-semibold text-token ml-1">{intlFractionNumber.format(totalBuyCost)}</span>
<img src={wellKnownExchangeRateDisplay.chaos_orb.img} alt={wellKnownExchangeRateDisplay.chaos_orb.alt} class="h-4 w-4 mt-1" />, earn
<span class="font-semibold text-secondary-300-600-token mx-1"
	>+{intlCompactNumber.format(totalExperience)}</span
>exp, sell for
<span class="font-semibold text-token ml-1">{intlFractionNumber.format(totalSellPrice)}</span
><img src={wellKnownExchangeRateDisplay.chaos_orb.img} alt={wellKnownExchangeRateDisplay.chaos_orb.alt} class="h-4 w-4 mt-1" />
