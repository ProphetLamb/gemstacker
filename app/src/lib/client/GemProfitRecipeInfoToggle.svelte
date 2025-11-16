<script>
	import { inspectProfit } from '$lib/client/gemProfitRecipeInfo';
	import { Icon } from '@steeze-ui/svelte-icon';
	import * as hi from '@steeze-ui/heroicons';
</script>

<button
	class="card p-2 variant-soft-surface flex flex-row items-center space-x-2 {!$inspectProfit.visible &&
	$inspectProfit.gem
		? ''
		: 'invisible opacity-0'} transition-all {$$props.class}"
	on:click={() => ($inspectProfit.visible = !$inspectProfit.visible)}
>
	<span>Recipie Info</span>
	<div class="button variant-filled-success rounded-full w-6 h-6 flex justify-center items-center">
		<Icon src={hi.ChevronLeft} size="18" />
	</div>
</button>
