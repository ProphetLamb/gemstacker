<script lang="ts">
	import GithubIcon from './GithubIcon.svelte';
	import { AppBar, getDrawerStore, type DrawerSettings } from '@skeletonlabs/skeleton';
	import { Icon } from '@steeze-ui/svelte-icon';
	import * as hi from '@steeze-ui/heroicons';
	import LocalSettings from '$lib/client/LocalSettings.svelte';
	import { menuNavLinks } from '$lib/links';
	import { page } from '$app/stores';

	const drawerStore = getDrawerStore();

	function drawerOpen(): void {
		const s: DrawerSettings = { id: 'root-sidenav' };
		drawerStore.open(s);
	}
	$: listboxItemActive = (href: string) =>
		$page.url.pathname?.includes(href) ? 'bg-primary-active-token' : '';
</script>

<AppBar padding="">
	<svelte:fragment slot="lead">
		<button on:click={drawerOpen} class="btn-icon btn-icon-sm md:!hidden">
			<Icon src={hi.Bars3} size="24" />
		</button>
		<img src="/favicon.png" alt="ico" class="w-8 h-8 hidden md:inline" />
		<a href="/" class="btn">
			<span class="text-3xl uppercase font-extrabold">Gem Stacker</span>
		</a>
		<div class="hidden md:flex">
			{#each menuNavLinks as { href, icon, title }}
				<a {href} class="{listboxItemActive(href)} btn hover:variant-soft-primary">
					<Icon src={icon} size="24" />
					<span class="">{title}</span>
				</a>
			{/each}
		</div>
	</svelte:fragment>
	<svelte:fragment slot="trail">
		<GithubIcon class="hidden sm:flex" />
		<LocalSettings class="hidden sm:flex" />
	</svelte:fragment>
</AppBar>
