<script lang="ts">
	import GemTradeQueryButton from '$lib/client/GemTradeQueryButton.svelte';
	import type { GemProfitResponseItem } from '$lib/gemLevelProfitApi';
	import GemTableIdentifier from '$lib/client/GemTableIdentifier.svelte';

	export let data: GemProfitResponseItem[];
</script>

<table class="list w-full border-separate border-spacing-y-2">
	<tbody>
		{#each data as gem, idx}
			<tr class="h-12">
				<GemTableIdentifier {gem} {idx} />
				<td class="pl-2">
					<GemTradeQueryButton gemPrice={gem} />
				</td>
			</tr>
		{/each}
	</tbody>
</table>
