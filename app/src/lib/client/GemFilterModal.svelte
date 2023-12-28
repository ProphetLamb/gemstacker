<script lang="ts">
	import GemFilterTable from './GemFilterTable.svelte';
	import { getModalStore } from '@skeletonlabs/skeleton';
	import { availableGems, type FilteredEvent } from './availableGems';
	import { Icon } from '@steeze-ui/svelte-icon';
	import * as hi from '@steeze-ui/heroicons';
	import { createEventDispatcher, onDestroy } from 'svelte';

	let filter = '';
	const modalStore = getModalStore();
	let maxDataCount = 10;
	$: selectedGems =
		!$availableGems || !filter
			? $availableGems ?? []
			: $availableGems?.filter((x) => x.name.includes(filter));
	$: data = firstN(selectedGems, maxDataCount);

	function firstN<T>(arr: T[], items: number): T[] {
		return arr.slice(0, Math.min(arr.length, items));
	}

	const loadMoreTriggerObserver = new IntersectionObserver((entries) => {
		if (entries.length === 0 || !entries[0].isIntersecting) {
			return;
		}
		maxDataCount += 10;
	});
	function loadMoreTrigger(e: HTMLDivElement) {
		loadMoreTriggerObserver.observe(e);
	}
	onDestroy(() => {
		loadMoreTriggerObserver.disconnect();
	});

	const dispatch = createEventDispatcher();

	function setFiltered(idx: number | number[], newValue: boolean | undefined) {
		if (!Array.isArray(idx)) {
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
			return;
		}
		const dataIndex = [];
		const oldValue = [];
		for (const i of idx) {
			const oldV = data[i].isFiltered;
			if (oldV === newValue) {
				continue;
			}
			dataIndex.push(i);
			oldValue.push(oldV);
			data[i].isFiltered = newValue;
		}

		dispatch('filtered', {
			dataIndex,
			gem: dataIndex.map((i) => data[i]),
			oldValue,
			newValue
		} satisfies FilteredEvent);
	}

	function setFilteredAll(value: boolean | undefined) {
		const gems = $availableGems;
		if (!gems || gems.length <= 0) {
			return;
		}
		const indicies = [...Array(gems.length).keys()];
		if (!filter) {
			setFiltered(indicies, value);
			return;
		}
		setFiltered(
			indicies.filter((i) => gems[i].name.includes(filter)),
			value
		);
	}
</script>

{#if $modalStore[0]}
	<div class="text-token flex flex-col items-center card">
		<div class="card-header w-full space-y-2">
			<div class="flex flex-row items-center gap-x-2">
				<Icon src={hi.MagnifyingGlass} size="22" />
				<input type="text" class="input" placeholder="Search..." bind:value={filter} />
			</div>
			<div class="flex flex-row-reverse items-center gap-x-2">
				<button
					class="btn btn-sm variant-outline-error text-error-400-500-token"
					on:click={() => setFilteredAll(undefined)}
				>
					<Icon src={hi.Trash} size="16" />
					<span class="">Exclude All</span>
				</button>
				<button
					class="btn btn-sm variant-outline-success text-success-400-500-token"
					on:click={() => setFilteredAll(true)}
				>
					<Icon src={hi.Check} size="16" />
					<span class="">Include All</span>
				</button>
				Selected
			</div>
		</div>
		<div class="card-body px-4 max-h-[calc(100vh-10rem)] overflow-y-scroll">
			<GemFilterTable {data} />
			{#if data.length === maxDataCount}
				<div class="align-middle w-full text-center pb-4" use:loadMoreTrigger>
					Search a gem name for more...
				</div>
			{/if}
		</div>
	</div>
{/if}
