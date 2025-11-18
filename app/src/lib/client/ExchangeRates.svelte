<script lang="ts">
	import {
		wellKnownExchangeRateDisplay,
		type WellKnownExchangeRateToChaosResponse
	} from '$lib/gemLevelProfitApi';
	import { intlFractionNumber } from '$lib/intl';

	export let exchange_rates: WellKnownExchangeRateToChaosResponse;
</script>

<div class={$$props.class ?? ''}>
	{#each Object.entries(wellKnownExchangeRateDisplay) as [key, display]}
		<label class="label">
			<span class="flex flex-row items-center"
				><img class="h-4 w-4 mt-[0.1875rem]" src={display.img} alt={display.alt} />
				{display.name}</span
			>
			<input
				class="input"
				type="number"
				readonly={true}
				name={key}
				value={intlFractionNumber.format(exchange_rates[key] || 1)}
			/>
		</label>
	{/each}
</div>
