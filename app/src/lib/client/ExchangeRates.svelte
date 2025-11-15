<script lang="ts">
	import { wellKnownExchangeRateDisplay, type WellKnownExchangeRateToChaosResponse } from "$lib/gemLevelProfitApi";
	import { intlFractionNumber } from "$lib/intl";

    export let exchange_rates: WellKnownExchangeRateToChaosResponse
</script>

<div class={$$props.class ?? ''}>
    {#each Object.entries(wellKnownExchangeRateDisplay) as [key, display]}
        <label class="lable">
            <span>{display}</span>
            <input class="input" type="number" readonly={true} name={key} value={intlFractionNumber.format(exchange_rates[key] || 1)}>
        </label>
    {/each}
</div>