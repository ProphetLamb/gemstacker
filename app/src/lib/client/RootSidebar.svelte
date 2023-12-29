<script lang="ts">
	import { AppRail, AppRailAnchor, getDrawerStore } from '@skeletonlabs/skeleton';
	import { Icon } from '@steeze-ui/svelte-icon';
	import * as hi from '@steeze-ui/heroicons';
	import { menuNavLinks } from '$lib/links';
	import { page } from '$app/stores';
	import GithubIcon from './GithubIcon.svelte';
	import LocalSettings from './LocalSettings.svelte';
	const drawerStore = getDrawerStore();

	function onClickAnchor(): void {
		drawerStore.close();
	}
	$: listboxItemActive = (href: string) =>
		$page.url.pathname?.includes(href) ? 'bg-primary-active-token' : '';
</script>

<div
	class="grid grid-cols-[auto_1fr] h-full bg-surface-50-900-token border-r border-surface-500/30 {$$props.class ??
		''}"
>
	<AppRail>
		<AppRailAnchor href="/" class="lg:hidden" on:click={onClickAnchor}>
			<svelte:fragment slot="lead">
				<Icon src={hi.Bars3} size="24" />
			</svelte:fragment>
			<span class="">Home</span>
		</AppRailAnchor>
	</AppRail>
	<section class="p-4 pb-20 space-y-4 overflow-y-auto">
		<!-- svelte-ignore a11y-no-noninteractive-element-interactions a11y-click-events-have-key-events -->
		<p class="font-bold pl-4 text-2xl" on:click={drawerStore.close}>Gem Stacker</p>
		<div class="">
			<GithubIcon />
			<LocalSettings />
		</div>
		<nav class="list-nav">
			<ul class="">
				{#each menuNavLinks as { href, icon, title }}
					<!-- svelte-ignore a11y-no-noninteractive-element-interactions -->
					<li on:keypress on:click={drawerStore.close} class="">
						<a {href} class={listboxItemActive(href)} data-sveltekit-preload-data="hover">
							<Icon src={icon} size="24" />
							<span class="">{title}</span>
						</a>
					</li>
				{/each}
			</ul>
		</nav>
	</section>
</div>
