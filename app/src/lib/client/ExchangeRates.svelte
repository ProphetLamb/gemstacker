<script lang="ts">
	import {
		wellKnownExchangeRateDisplay,
		type WellKnownExchangeRateToChaosResponse
	} from '$lib/gemLevelProfitApi';
	import { intlFractionNumber } from '$lib/intl';
	import type { CssClasses } from '@skeletonlabs/skeleton';
	import CurrencyIcon from './CurrencyIcon.svelte';

	export let li_class: CssClasses | undefined | null = undefined;
	export let exchange_rates: Partial<WellKnownExchangeRateToChaosResponse>;
</script>

<ul class={$$props.class ?? ''}>
	{#each Object.entries(wellKnownExchangeRateDisplay) as [key, display]}
		{@const rate = exchange_rates[key]}
		{#if rate}
			<li class={li_class ?? ''}>
				<div class="flex flex-row items-center">
					<CurrencyIcon {...display} />
					<span>{display.title}</span>
				</div>
				<input
					class="input"
					type="number"
					readonly={true}
					name={key}
					value={intlFractionNumber.format(rate)}
				/>
			</li>
		{/if}
	{/each}
</ul>
