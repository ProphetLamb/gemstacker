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
				<span>1</span>
				<img
					class="h-4 w-4 mt-[0.1875rem]"
					src={wellKnownExchangeRateDisplay.divine_orb.img}
					alt={wellKnownExchangeRateDisplay.divine_orb.alt}
				/>
				<span>=</span>
				<span>{$exchangeRates.divine_orb}</span>
				<img
					class="h-4 w-4 mt-[0.1875rem]"
					src={wellKnownExchangeRateDisplay.chaos_orb.img}
					alt={wellKnownExchangeRateDisplay.chaos_orb.alt}
				/>
			</button>
			<div class="card p-2 shadow-xl" data-popup="popupExchangeRates">
				<ExchangeRates exchange_rates={$exchangeRates} />
			</div>
		{/if}
		<GithubIcon class="hidden sm:flex" />
		<LocalSettings class="hidden sm:flex" />
	</svelte:fragment>
</AppBar>
