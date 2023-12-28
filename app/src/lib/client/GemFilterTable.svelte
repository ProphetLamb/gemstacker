<script lang="ts">
	import type { AvailableGem, FilteredEvent } from '$lib/client/availableGems.ts';
	import GemTableIdentifier from '$lib/client/GemTableIdentifier.svelte';
	import { Icon } from '@steeze-ui/svelte-icon';
	import * as hi from '@steeze-ui/heroicons';
	import { createEventDispatcher } from 'svelte';

	export let data: AvailableGem[];
	$: data;

	const dispatch = createEventDispatcher();

	function setFiltered(idx: number, newValue: boolean | undefined) {
		const oldValue = data[idx].isFiltered;
		if (oldValue === newValue) {
			return;
		}
		data[idx].isFiltered = newValue;

		dispatch('filtered', {
			dataIndex: idx,
			gem: data[idx],
			oldValue,
			newValue
		} satisfies FilteredEvent);
	}
</script>

<table class="list border-separate border-spacing-y-2">
	<tbody>
		{#each data as gem, idx}
			<tr class="h-12 {gem.isFiltered ? '[&>td]:bg-error-600/20' : ''} ">
				<GemTableIdentifier {gem} {idx} />
				<td class="pl-2">
					{#if gem.isFiltered}
						<button class="btn variant-soft-success" on:click={() => setFiltered(idx, undefined)}>
							<Icon src={hi.Check} size="22" />
						</button>
					{:else}
						<button class="btn variant-soft-error" on:click={() => setFiltered(idx, true)}>
							<Icon src={hi.Trash} size="22" />
						</button>
					{/if}
				</td>
			</tr>
		{/each}
	</tbody>
</table>

<style lang="postcss">
	tr:first-child {
		@apply rounded-s-full;
	}
	tr:last-child {
		@apply rounded-s-full;
	}
</style>
