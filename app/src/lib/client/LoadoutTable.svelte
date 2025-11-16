<script lang="ts">
	import GemSocket from './GemSocket.svelte';
	import GemTradeQueryButton from '$lib/client/GemTradeQueryButton.svelte';
	import type { LoadoutResponseItem } from '$lib/loadout';
	import GemTableIdentifier from '$lib/client/GemTableIdentifier.svelte';
	import { inspectProfit } from './gemProfitRecipeInfo';

	export let data: LoadoutResponseItem[];
	$: data;
</script>

<table class="list w-full border-separate border-spacing-y-2">
	<tbody>
		{#each data as { gem, socket }, idx}
			<tr
				class="h-12"
				on:mouseover={() => ($inspectProfit.gem = gem)}
				on:focus={() => ($inspectProfit.gem = gem)}
			>
				<GemTableIdentifier {gem} {idx} />
				<td> <span class="font-semibold text-surface-600-300-token">&#215;</span></td>
				<td class="pl-2">
					<div class="flex flex-col md:flex-row gap-x-1 items-center">
						{#each socket as { color, count }}
							<GemSocket class="flex justify-center items-center w-6 h-6 shadow-sm pb-0.5" {color}
								>{count}</GemSocket
							>
						{/each}
					</div>
				</td>
				<td class="pl-2">
					<GemTradeQueryButton gemPrice={gem} />
				</td>
			</tr>
		{/each}
	</tbody>
</table>
