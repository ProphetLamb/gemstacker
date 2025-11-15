<script lang="ts">
	import { browser } from '$app/environment';
	import { currencyModValues, currencyRerollRare } from '$lib/knownImages';
	import type { CurrencyDisplay } from '$lib/client/currency';
	import { localSettings } from '$lib/client/localSettings';
	import { exchangeRates } from '$lib/client/exchangeRates';
	import { intlWholeNumber } from '$lib/intl';
	import type { CssClasses } from '@skeletonlabs/skeleton';
	import Currency from './Currency.svelte';

	export let value;
    export let value_class: CssClasses | undefined | null = undefined;
	export let currency_display: CurrencyDisplay | undefined | null = undefined;
	export let img_class: CssClasses | undefined | null = undefined;

	$: actualCurrencyDisplay = currency_display ?? $localSettings.currency_display;
</script>

{#if browser}
    {#if actualCurrencyDisplay === 'ChaosFaction' || !$exchangeRates}
        <Currency
            {value}
            {value_class}
            number_format={intlWholeNumber}
            alt="c"
            src={currencyRerollRare}
            {img_class}
        />
    {:else if actualCurrencyDisplay === 'DivineFraction'}
        <Currency value={value / $exchangeRates.divine_orb} alt="d" src={currencyModValues} />
    {:else}
        {#if value / $exchangeRates.divine_orb > 1}
            <Currency
                value={value / $exchangeRates.divine_orb}
                {value_class}
                number_format={intlWholeNumber}
                alt="d"
                src={currencyModValues}
                {img_class}
            />
        {/if}
        <Currency
            value={value - Math.floor(value / $exchangeRates.divine_orb) * $exchangeRates.divine_orb}
            {value_class}
            number_format={intlWholeNumber}
            alt="c"
            src={currencyRerollRare}
            {img_class}
        />
    {/if}
{:else}
    <Currency
        {value}
        {value_class}
        number_format={intlWholeNumber}
        alt="c"
        src={currencyRerollRare}
        {img_class}
    />
{/if}
