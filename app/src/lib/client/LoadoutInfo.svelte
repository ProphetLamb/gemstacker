<script lang="ts">
	import { intlCompactNumber, intlFractionNumber } from '$lib/intl';
	import { currencyRerollRare } from '$lib/knownImages';
	import type { LoadoutResponse } from '$lib/loadout';
	import { getModalStore, type ModalSettings } from '@skeletonlabs/skeleton';
	import GemFilterModal from './GemFilterModal.svelte';
	import * as hi from '@steeze-ui/heroicons';
	import { Icon } from '@steeze-ui/svelte-icon';

	export let loadout: LoadoutResponse;
	$: totalBuyCost = loadout.totalBuyCost;
	$: totalExperience = loadout.totalExperience;
	$: totalSellPrice = loadout.totalSellPrice;
	$: count = loadout.count;

	const modalStore = getModalStore();
	function gemFilterModal() {
		const modal: ModalSettings = {
			type: 'component',
			component: { ref: GemFilterModal },
			title: 'Filter unwanted gems',
			body: ''
		};
		modalStore.trigger(modal);
	}
</script>

<div class="flex flex-row justify-between w-full text-surface-600-300-token">
	<p class="flex flex-row items-center">
		Buy <span class="font-semibold text-token mx-1">{count}</span>gems for
		<span class="font-semibold text-token ml-1">{intlFractionNumber.format(totalBuyCost)}</span>
		<img src={currencyRerollRare} alt="c" class="h-4 w-4 mt-1" />, earn
		<span class="font-semibold text-secondary-300-600-token mx-1"
			>+{intlCompactNumber.format(totalExperience)}</span
		>exp, sell for
		<span class="font-semibold text-token ml-1">{intlFractionNumber.format(totalSellPrice)}</span
		><img src={currencyRerollRare} alt="c" class="h-4 w-4 mt-1" />
	</p>

	<button
		type="button"
		class="btn btn-sm text-token variant-ghost-tertiary shadow"
		on:click={gemFilterModal}
	>
		<Icon src={hi.Funnel} size="16" />
		<span class="mr-0 5">Filter</span>
	</button>
</div>
