<script lang="ts">
	import RootSidebar from './RootSidebar.svelte';
	import { getDrawerStore, Drawer } from '@skeletonlabs/skeleton';

	const drawerStore = getDrawerStore();

	$: classesDrawer = $drawerStore.id === 'root-sidenav' ? 'lg:hidden' : '';
</script>

<Drawer class={classesDrawer}>
	{#if $drawerStore.id === 'root-sidenav'}
		<RootSidebar />
	{:else}
		<!-- Fallback Error -->
		<div class="w-full h-full flex justify-center items-center">
			<div class="text-center space-y-2">
				<p>Invalid <code class="code">$drawerStore.id</code> provided.</p>
			</div>
		</div>
	{/if}
</Drawer>
