<script lang="ts">
	import {
		wellKnownExchangeRateDisplay,
		type WellKnownExchangeRateToChaosResponse
	} from '$lib/gemLevelProfitApi';
	import { intlFractionNumber } from '$lib/intl';
	import type { CssClasses } from '@skeletonlabs/skeleton';

	export let li_class: CssClasses | undefined | null = undefined;
	export let exchange_rates: Partial<WellKnownExchangeRateToChaosResponse>;
</script>

<ul class={$$props.class ?? ''}>
	{#each Object.entries(wellKnownExchangeRateDisplay) as [key, display]}
		{@const rate = exchange_rates[key]}
		{#if rate}
			<li class={li_class ?? ''}>
				<div class="flex flex-row items-center">
					<img class="h-4 w-4 mt-[0.1875rem]" src={display.img} alt={display.alt} />
					<span>{display.name}</span>
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
