<script lang="ts">
	import { inspectProfit } from './gemProfitRecipeInfo';
	import GemProfitRecipeInfo from './GemProfitRecipeInfo.svelte';
</script>

<GemProfitRecipeInfo
	class="w-72 {$inspectProfit.visible && $inspectProfit.gem
		? ''
		: 'hidden'} transition-all"
	gem={$inspectProfit.gem}
	close={() => ($inspectProfit.visible = false)}
/>
