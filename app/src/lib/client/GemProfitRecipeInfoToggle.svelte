<script>
	import { inspectProfit } from '$lib/client/gemProfitRecipeInfo';
	import { Icon } from '@steeze-ui/svelte-icon';
	import * as hi from '@steeze-ui/heroicons';
</script>

<button
	class="z-[5] card p-2 variant-soft-surface hidden flex-row items-center space-x-2 {$inspectProfit.gem
		? '!flex'
		: ''} {!$inspectProfit.visible ? '' : 'invisible opacity-0'} transition-all {$$props.class}"
	on:click={() => ($inspectProfit.visible = !$inspectProfit.visible)}
>
	<span>Recipe Info</span>
	<button type="button" class="btn-icon variant-filled-success w-8 h-8"
		><Icon src={hi.ChevronLeft} size="18" /></button
	>
</button>
