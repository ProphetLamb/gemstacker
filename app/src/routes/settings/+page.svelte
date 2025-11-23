<script lang="ts">
	import { Icon } from '@steeze-ui/svelte-icon';
	import * as hi from '@steeze-ui/heroicons';
	import LocalSettingsBasic from '$lib/client/LocalSettingsBasic.svelte';
	import GemFilterTable from '$lib/client/GemFilterTable.svelte';
	import { availableGems } from '$lib/client/availableGems';
	import { localSettings } from '$lib/client/localSettings';
	import ExcludeRecipes from '$lib/client/ExcludeRecipes.svelte';
</script>

<article
	class="article m-auto lg:max-w-screen-lg flex flex-col items-center justify-center space-y-4 text-center"
>
	<h1 class="h1 flex flex-row items-center space-x-2">
		<Icon src={hi.Cog6Tooth} size="44" class="-mb-1" theme="solid" /> <span>Settings</span>
	</h1>
	<section class="card">
		<h2 class="h2">Basics</h2>
		<LocalSettingsBasic />
	</section>

	<section class="card">
		<h2 class="h2">Exclude Recipes</h2>
		<ExcludeRecipes
			bind:excludedRecipes={$localSettings.disallowed_recipes}
		/>
	</section>

	<section class="card">
		<h2 class="h2">Exclude Gems</h2>
		{#if !$availableGems?.length}
			<p>Unable to remove gems from excludsion. No data has been loaded</p>
			<p>Load data first</p>
		{:else}
			<GemFilterTable class="overflow-y-scroll" data={$availableGems} />
		{/if}
	</section>
</article>

<style lang="postcss">
	.article > section {
		@apply p-4 space-y-2 shadow-xl w-full;
		& > h2 {
			@apply mb-4;
		}
	}
</style>
