<script lang="ts">
	import type { FilteredEvent } from '$lib/client/availableGems';
	import GemTableIdentifier from '$lib/client/GemTableIdentifier.svelte';
	import { Icon } from '@steeze-ui/svelte-icon';
	import * as hi from '@steeze-ui/heroicons';
	import { createEventDispatcher } from 'svelte';
	import type { GemProfitResponse } from '$lib/gemLevelProfitApi';
	import { localSettings } from './localSettings';

	export let data: GemProfitResponse;
	$: data;
	$: excludedGems = new Set($localSettings.exclude_gems);

	const dispatch = createEventDispatcher<FilteredEvent>();

	function setExcluded(idx: number, newValue: boolean) {
		const oldValue = excludedGems.has(data[idx].name.toLowerCase());
		if (oldValue === newValue) {
			return;
		}
		dispatch('filtered', {
			dataIndex: idx,
			gem: data[idx],
			oldValue,
			newValue
		});
	}
</script>

<table class="list border-separate border-spacing-y-2">
	<tbody>
		{#each data as gem, idx}
			{@const isExcluded = excludedGems.has(gem.name.toLowerCase())}
			<tr class="h-12 {isExcluded ? '[&>td]:bg-error-600/20' : ''} ">
				<GemTableIdentifier {gem} {idx} />
				<td class="pl-2">
					{#if isExcluded}
						<button class="btn variant-soft-success" on:click={() => setExcluded(idx, false)}>
							<Icon src={hi.Check} size="22" />
						</button>
					{:else}
						<button class="btn variant-soft-error" on:click={() => setExcluded(idx, true)}>
							<Icon src={hi.Trash} size="22" />
						</button>
					{/if}
				</td>
			</tr>
		{/each}
	</tbody>
</table>

<style lang="postcss">
</style>
