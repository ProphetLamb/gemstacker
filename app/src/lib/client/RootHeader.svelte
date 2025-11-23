<script lang="ts">
	import GithubIcon from './GithubIcon.svelte';
	import {
		AppBar,
		getDrawerStore,
		popup,
		type DrawerSettings,
		type PopupSettings
	} from '@skeletonlabs/skeleton';
	import { Icon } from '@steeze-ui/svelte-icon';
	import * as hi from '@steeze-ui/heroicons';
	import LocalSettings from '$lib/client/LocalSettings.svelte';
	import { menuNavLinks } from '$lib/navLinks';
	import { page } from '$app/stores';
	import ExchangeRates from './ExchangeRates.svelte';
	import { exchangeRates } from './exchangeRates';
	import { wellKnownExchangeRateDisplay } from '$lib/gemLevelProfitApi';
	import CurrencyIcon from './CurrencyIcon.svelte';

	const drawerStore = getDrawerStore();

	function drawerOpen(): void {
		const s: DrawerSettings = { id: 'root-sidenav' };
		drawerStore.open(s);
	}
	$: listboxItemActive = (href: string) =>
		$page.url.pathname?.includes(href) ? 'bg-primary-active-token' : '';

	const popupExchangeRates: PopupSettings = {
		event: 'focus-click',
		target: 'popupExchangeRates',
		placement: 'bottom'
	};
</script>

<AppBar padding="">
	<svelte:fragment slot="lead">
		<button on:click={drawerOpen} class="btn-icon btn-icon-sm md:!hidden ml-4">
			<Icon src={hi.Bars3} size="24" />
		</button>
		<img src="/favicon.png" alt="ico" class="w-8 h-8 hidden md:inline ml-4" />
		<a href="/" class="btn">
			<span class="text-3xl uppercase font-extrabold">Gem Stacker</span>
		</a>
		<div class="hidden md:flex">
			{#each menuNavLinks as { href, icon, title, class: clazz, decoration }}
				<a {href} class="{listboxItemActive(href)} btn hover:variant-soft-primary {clazz || ''}">
					{#if icon}
						<Icon src={icon} size="24" />
					{/if}
					<span>{title}</span>
					{#if decoration}
						<button class={decoration.class}>
							{#if decoration.icon}
								<Icon src={decoration.icon} size="16" />
							{/if}
							<span>{decoration.title}</span>
						</button>
					{/if}
				</a>
			{/each}
		</div>
	</svelte:fragment>
	<svelte:fragment slot="trail">
		{#if $exchangeRates}
			<button class="btn btn-sm variant-soft-tertiary flex flex-row space-x-0" use:popup={popupExchangeRates}>
				<span class="max-lg:hidden">1</span>
				<CurrencyIcon
					class="max-lg:hidden"
					{...wellKnownExchangeRateDisplay.divine_orb}
				 />
				<span class="max-lg:hidden">=</span>
				<span>{$exchangeRates.divine_orb}</span>
				<CurrencyIcon {...wellKnownExchangeRateDisplay.chaos_orb} />
			</button>
			<div class="card p-4 shadow-xl w-[calc(100%-2rem)] md:w-96 text-center" data-popup="popupExchangeRates">
				<h3 class="h3">Exchange rates</h3>
				<ExchangeRates exchange_rates={$exchangeRates} />
			</div>
		{/if}
		<GithubIcon class="hidden lg:flex" />
		<LocalSettings class="hidden sm:flex" />
	</svelte:fragment>
</AppBar>
