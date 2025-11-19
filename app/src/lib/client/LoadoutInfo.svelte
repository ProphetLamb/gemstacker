<script lang="ts">
	import { intlCompactNumber } from '$lib/intl';
	import type { LoadoutResponse } from '$lib/loadout';
	import Chaos from './Chaos.svelte';

	export let loadout: LoadoutResponse;
	$: totalBuyCost = loadout.totalBuyCost;
	$: totalExperience = loadout.totalExperience;
	$: totalSellPrice = loadout.totalSellPrice;
	$: count = loadout.count;
</script>

Buy <span class="font-semibold text-token mx-1">{count}</span>gems for&nbsp;<Chaos
	value={totalBuyCost}
/>, earn
<span class="font-semibold text-secondary-300-600-token mx-1"
	>+{intlCompactNumber.format(totalExperience)}</span
>exp, sell for&nbsp;<Chaos value={totalSellPrice} />
