<script lang="ts">
	import { browser } from '$app/environment';
	import type { CurrencyDisplay } from '$lib/currency';
	import { localSettings } from '$lib/client/localSettings';
	import { exchangeRates } from '$lib/client/exchangeRates';
	import { intlWholeNumber } from '$lib/intl';
	import type { CssClasses } from '@skeletonlabs/skeleton';
	import Currency from './Currency.svelte';
	import { wellKnownExchangeRateDisplay } from '$lib/gemLevelProfitApi';

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
			alt={wellKnownExchangeRateDisplay.chaos_orb.alt}
			src={wellKnownExchangeRateDisplay.chaos_orb.img}
			{img_class}
		/>
	{:else if actualCurrencyDisplay === 'DivineFraction'}
		<Currency value={value / $exchangeRates.divine_orb} alt={wellKnownExchangeRateDisplay.divine_orb.alt} src={wellKnownExchangeRateDisplay.divine_orb.img} />
	{:else}
		{@const divineOrb = Math.floor($exchangeRates.divine_orb)}
		{@const chaosOrb =
			Math.sign(value) * (Math.abs(value) - Math.floor(Math.abs(value) / divineOrb) * divineOrb)}
		{#if value / divineOrb >= 1}
			<Currency
				value={value / divineOrb}
				{value_class}
				number_format={intlWholeNumber}
			alt={wellKnownExchangeRateDisplay.divine_orb.alt}
			src={wellKnownExchangeRateDisplay.divine_orb.img}
				{img_class}
			/>
		{/if}
		<Currency
			value={chaosOrb ?? 0}
			{value_class}
			number_format={intlWholeNumber}
			alt={wellKnownExchangeRateDisplay.chaos_orb.alt}
			src={wellKnownExchangeRateDisplay.chaos_orb.img}
			{img_class}
		/>
	{/if}
{:else}
	<Currency
		{value}
		{value_class}
		number_format={intlWholeNumber}
		alt={wellKnownExchangeRateDisplay.chaos_orb.alt}
		src={wellKnownExchangeRateDisplay.chaos_orb.img}
		{img_class}
	/>
{/if}
