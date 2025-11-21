<script lang="ts">
	import { browser } from '$app/environment';
	import type { CurrencyDisplay } from '$lib/currency';
	import { localSettings } from '$lib/client/localSettings';
	import { exchangeRates } from '$lib/client/exchangeRates';
	import { intlWholeNumber } from '$lib/intl';
	import type { CssClasses } from '@skeletonlabs/skeleton';
	import Currency from './Currency.svelte';
	import { wellKnownExchangeRateDisplay } from '$lib/gemLevelProfitApi';

	export let value_prefix: number | string | undefined | null = undefined;
	export let value;
	export let value_class: CssClasses | undefined | null = undefined;
	export let currency_display: CurrencyDisplay | undefined | null = undefined;
	export let img_class: CssClasses | undefined | null = undefined;

	$: actualCurrencyDisplay = currency_display ?? $localSettings.currency_display;
</script>

{#if browser}
	{#if actualCurrencyDisplay === 'ChaosFaction' || !$exchangeRates}
		<Currency
			{value_prefix}
			{value}
			{value_class}
			number_format={intlWholeNumber}
			alt={wellKnownExchangeRateDisplay.chaos_orb.alt}
			src={wellKnownExchangeRateDisplay.chaos_orb.img}
			{img_class}
		/>
	{:else if actualCurrencyDisplay === 'DivineFraction'}
		<Currency
			{value_prefix}
			value={value / $exchangeRates.divine_orb}
			{value_class}
			alt={wellKnownExchangeRateDisplay.divine_orb.alt}
			src={wellKnownExchangeRateDisplay.divine_orb.img}
		/>
	{:else if actualCurrencyDisplay === 'ChaosThenDivieFraction'}
		{@const divineOrb = Math.floor($exchangeRates.divine_orb)}
		{#if Math.abs(value / divineOrb) >= 0.5}
		<Currency
			{value_prefix}
			value={value / $exchangeRates.divine_orb}
			{value_class}
			alt={wellKnownExchangeRateDisplay.divine_orb.alt}
			src={wellKnownExchangeRateDisplay.divine_orb.img}
		/>
		{:else}
			<Currency
				{value_prefix}
				{value}
				{value_class}
				number_format={intlWholeNumber}
				alt={wellKnownExchangeRateDisplay.chaos_orb.alt}
				src={wellKnownExchangeRateDisplay.chaos_orb.img}
				{img_class}
			/>
		{/if}
	{:else}
		{@const divineOrb = Math.floor($exchangeRates.divine_orb)}
		{@const chaosOrb =
			Math.sign(value) * (Math.abs(value) - Math.floor(Math.abs(value) / divineOrb) * divineOrb)}
		{#if Math.abs(value / divineOrb) >= 1}
			<Currency
				{value_prefix}
				value={value / divineOrb}
				{value_class}
				number_format={intlWholeNumber}
				alt={wellKnownExchangeRateDisplay.divine_orb.alt}
				src={wellKnownExchangeRateDisplay.divine_orb.img}
				{img_class}
			/>
		{/if}
		<Currency
			value_prefix={Math.abs(value / divineOrb) >= 1 ? value_prefix : ''}
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
		{value_prefix}
		{value}
		{value_class}
		number_format={intlWholeNumber}
		alt={wellKnownExchangeRateDisplay.chaos_orb.alt}
		src={wellKnownExchangeRateDisplay.chaos_orb.img}
		{img_class}
	/>
{/if}
