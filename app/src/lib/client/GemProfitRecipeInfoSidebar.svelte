<script lang="ts">
	import { Icon } from '@steeze-ui/svelte-icon';
	import * as hi from '@steeze-ui/heroicons';
	import { inspectProfit } from './gemProfitRecipeInfo';
	import GemProfitRecipeInfo from './GemProfitRecipeInfo.svelte';
	import type { CssClasses } from '@skeletonlabs/skeleton';
	import { onDestroy } from 'svelte';
	export let contentWidth: CssClasses
	export let showAside: CssClasses = 'xl'

	onDestroy(() => {
		$inspectProfit.gem = undefined
	})
</script>

<div class="w-fit {showAside}:w-[{contentWidth}] max-w-full">
<div class="sticky pointer-events-auto top-0 left-full {showAside}:left-[calc(100%-21.25rem)] flex items-center align-middle right-0 w-8 h-8 z-10 {!$inspectProfit.visible && !!$inspectProfit.gem
		? ''
		: 'invisible opacity-0'} transition-opacity">
	<button
		class="button variant-ghost rounded-full w-6 h-6 flex justify-center items-center"
		on:click={() => ($inspectProfit.visible = !$inspectProfit.visible)}><Icon src={hi.ChevronRight}/></button
	>
</div>
<GemProfitRecipeInfo
	class="sticky pointer-events-auto top-8 left-full right-0 w-72 h-96 -translate-y-8 z-10 {$inspectProfit.visible && !!$inspectProfit.gem
		? ''
		: 'invisible opacity-0'} transition-opacity"
	gem={$inspectProfit.gem}
	close={() => ($inspectProfit.visible = false)}
/>
<div class=" mt-[-26rem] block" />
	<slot/>
</div>