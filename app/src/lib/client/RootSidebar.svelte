<script lang="ts">
	import { AppRail, AppRailAnchor, getDrawerStore } from '@skeletonlabs/skeleton';
	import { Icon } from '@steeze-ui/svelte-icon';
	import * as hi from '@steeze-ui/heroicons';
	import { menuNavLinks } from '$lib/links';
	import { page } from '$app/stores';
	import GithubIcon from './GithubIcon.svelte';
	import LocalSettings from './LocalSettings.svelte';
	const drawerStore = getDrawerStore();

	function drawerClose(): void {
		drawerStore.close();
	}
	$: listboxItemActive = (href: string) =>
		$page.url.pathname?.includes(href) ? 'bg-primary-active-token' : '';
</script>

<div
	class="grid grid-cols-[auto_1fr] h-full bg-surface-50-900-token border-r border-surface-500/30 {$$props.class ??
		''}"
>
	<AppRail width="0" />
	<section class="pb-20 space-y-4 overflow-y-auto">
		<div class="p-4 bg-surface-100-800-token">
			<div class="py-2 space-x-4">
				<button class="btn-icon btn-icon-sm" on:click={drawerClose}>
					<Icon src={hi.Bars3} size="24" />
				</button>
				<a href="/" class="text-3xl uppercase font-extrabold" on:click={drawerClose}>Gem Stacker</a>
			</div>
		</div>
		<div class="px-4">
			<GithubIcon />
			<LocalSettings />
		</div>
		<nav class="px-4 list-nav">
			<ul class="">
				{#each menuNavLinks as { href, icon, title }}
					<li>
						<a {href} class={listboxItemActive(href)} on:click={drawerClose}>
							<Icon src={icon} size="24" />
							<span class="">{title}</span>
						</a>
					</li>
				{/each}
			</ul>
		</nav>
	</section>
</div>
