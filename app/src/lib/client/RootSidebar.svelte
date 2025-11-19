<script lang="ts">
	import { AppRail, getDrawerStore } from '@skeletonlabs/skeleton';
	import { Icon } from '@steeze-ui/svelte-icon';
	import { menuNavLinks } from '$lib/navLinks';
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
		<div class="bg-surface-100-800-token">
			<div class="pt-2 pb-2 space-x-4 flex flex-row items-center justify-start w-full">
				<button class="btn-icon btn-icon-sm" on:click={drawerClose}>
					<img src="/favicon.png" alt="ico" class="w-8 h-8" />
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
							{#if icon}
								<Icon src={icon} size="24" />
							{/if}
							<span class="">{title}</span>
						</a>
					</li>
				{/each}
			</ul>
		</nav>
	</section>
</div>
