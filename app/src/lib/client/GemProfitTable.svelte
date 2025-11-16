<script lang="ts">
	import GemTradeQueryButton from '$lib/client/GemTradeQueryButton.svelte';
	import type { GemProfitResponseItem } from '$lib/gemLevelProfitApi';
	import GemTableIdentifier from '$lib/client/GemTableIdentifier.svelte';
	import { inspectProfit } from '$lib/client/gemProfitRecipeInfo';
	import { onDestroy } from 'svelte';

	export let data: GemProfitResponseItem[];
	onDestroy(() => {
		$inspectProfit.gem = undefined;
	});
</script>

<table class="list w-full border-separate border-spacing-y-2">
	<tbody>
		{#each data as gem, idx}
			<tr
				class="h-12 hover:brightness-110"
				on:mouseover={() => ($inspectProfit.gem = gem)}
				on:focus={() => ($inspectProfit.gem = gem)}
			>
				<GemTableIdentifier {gem} {idx} />
				<td class="pl-2 hidden sm:table-cell">
					<GemTradeQueryButton gemPrice={gem} />
				</td>
			</tr>
		{/each}
	</tbody>
</table>
