<script lang="ts">
	import type { GemProfitProbabilisticProfitMargin } from '$lib/gemLevelProfitApi';
	import { intlFixed2Number } from '$lib/intl';
	import { profitToTextColor, wellKnownProbabilisticLabelDisplay } from '$lib/recipe';
	import Chaos from './Chaos.svelte';
	export let probabilities: GemProfitProbabilisticProfitMargin[] | undefined | null;
</script>

<ul class={$$props.class ?? ''}>
	{#each probabilities ?? [] as prob}
		{@const label = wellKnownProbabilisticLabelDisplay[prob.label ?? 'misc']}
		<li class="flex flex-col items-start align-middle w-full">
			<span>{label}</span>
			<div class="ml-auto flex flex-row items-end text-sm">
				<span>{intlFixed2Number.format(prob.chance * 100)}% for&nbsp;</span>
				<Chaos value_prefix={prob.earnings > 0 ? '+' : ''} value_class="{profitToTextColor(prob.earnings)}" value={prob.earnings} />
			</div>
		</li>
		<hr />
	{/each}
</ul>

<style lang="postcss">
	hr:last-child {
		@apply hidden;
	}
</style>
