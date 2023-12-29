<script lang="ts">
	import GithubIcon from './GithubIcon.svelte';
	import { AppBar, getDrawerStore, type DrawerSettings } from '@skeletonlabs/skeleton';
	import { Icon } from '@steeze-ui/svelte-icon';
	import * as hi from '@steeze-ui/heroicons';
	import LocalSettings from '$lib/client/LocalSettings.svelte';
	import { menuNavLinks } from '$lib/links';

	const drawerStore = getDrawerStore();

	function drawerOpen(): void {
		const s: DrawerSettings = { id: 'root-sidenav' };
		drawerStore.open(s);
	}
</script>

<AppBar>
	<svelte:fragment slot="lead">
		<button on:click={drawerOpen} class="btn-icon btn-icon-sm md:!hidden">
			<Icon src={hi.Bars3} size="24" />
		</button>
		<a href="/" class="btn text-3xl uppercase font-extrabold"> Gem Stacker </a>
		<div class="hidden md:flex">
			{#each menuNavLinks as { href, icon, title }}
				<a {href} class="btn hover:variant-soft-primary">
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
