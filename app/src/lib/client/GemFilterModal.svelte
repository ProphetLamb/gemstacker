<script lang="ts">
	import GemFilterTable from './GemFilterTable.svelte';
	import { getModalStore } from '@skeletonlabs/skeleton';
	import { availableGems } from './availableGems';
	import { Icon } from '@steeze-ui/svelte-icon';
	import * as hi from '@steeze-ui/heroicons';
	import { onDestroy, tick } from 'svelte';
	import { localSettings } from './localSettings';

	const lazyLoadIncrement = 10;

	let filter = '';
	const modalStore = getModalStore();
	let maxDataCount = 0;
	$: selectedGems =
		!$availableGems || !filter
			? $availableGems ?? []
			: $availableGems?.filter((x) => x.name.toLowerCase().includes(filter.toLowerCase()));
	$: data = firstN(selectedGems, maxDataCount);

	function firstN<T>(arr: T[], items: number): T[] {
		return arr.slice(0, Math.min(arr.length, items));
	}

	const loadMoreTriggerObserver = new IntersectionObserver((entries) => {
		if (entries.length === 0 || !entries[0].isIntersecting) {
			return;
		}
		maxDataCount += lazyLoadIncrement;
	});
	function loadMoreTrigger(e: HTMLDivElement) {
		Promise.resolve().then(async () => {
			while (maxDataCount < lazyLoadIncrement * 2) {
				await tick();
				await new Promise((resolve) => setTimeout(resolve, 100));
				if (!(e.offsetWidth || e.offsetHeight || e.getClientRects().length)) {
					break;
				}
				maxDataCount += lazyLoadIncrement;
				await tick();
			}
			loadMoreTriggerObserver.observe(e);
		});
	}
	onDestroy(() => {
		loadMoreTriggerObserver.disconnect();
	});

	function setExcluded(idx: number | number[], newValue: boolean) {
		const gems = selectedGems ?? [];
		const excludedGems = new Set($localSettings.exclude_gems);
		if (!Array.isArray(idx)) {
			const gemName = gems[idx].name.toLowerCase();
			if (excludedGems.has(gemName) && !newValue) {
				excludedGems.delete(gemName);
			}
			if (!excludedGems.has(gemName) && newValue) {
				excludedGems.add(gemName);
			}
		} else {
			for (const i of idx) {
				const gemName = gems[i].name.toLowerCase();
				if (excludedGems.has(gemName) && !newValue) {
					excludedGems.delete(gemName);
				}
				if (!excludedGems.has(gemName) && newValue) {
					excludedGems.add(gemName);
				}
			}
		}
		$localSettings.exclude_gems = [...excludedGems];
	}

	function setExcludedAll(value: boolean) {
		const gems = $availableGems;
		if (!gems || gems.length <= 0) {
			return;
		}
		const indicies = [...Array(gems.length).keys()];
		if (!filter) {
			setExcluded(indicies, value);
			return;
		}
		setExcluded(
			indicies.filter((i) => gems[i].name.includes(filter)),
			value
		);
	}
</script>

{#if $modalStore[0]}
	<div
		class="text-token flex flex-col items-center card max-h-[calc(100vh-2rem)] max-w-[100vw] py-2 space-y-2"
	>
		<div class="flex flex-row px-2 space-x-2 w-full justify-between items-center">
			<h1 class="h1">Exclude gems</h1>
			<button
				type="button"
				class="button variant-ghost rounded-full w-8 h-8 flex justify-center items-center"
				on:click={() => modalStore.close()}><Icon src={hi.XMark} size="22" /></button
			>
		</div>
		<slot />
		<div class="card-header w-full space-y-2">
			<div class="flex flex-row items-center gap-x-2">
				<Icon src={hi.MagnifyingGlass} size="22" />
				<input type="text" class="input" placeholder="Search..." bind:value={filter} />
			</div>
			<div class="flex flex-row items-center gap-x-2">
				Selected
				<button
					class="btn btn-sm variant-outline-success text-success-400-500-token"
					on:click={() => setExcludedAll(false)}
				>
					<Icon src={hi.Check} size="16" />
					<span class="">Include All</span>
				</button>
				<button
					class="btn btn-sm variant-outline-error text-error-400-500-token"
					on:click={() => setExcludedAll(true)}
				>
					<Icon src={hi.Trash} size="16" />
					<span class="">Exclude All</span>
				</button>
			</div>
		</div>
		<div class="card-body px-4 overflow-y-auto max-w-full">
			<GemFilterTable
				on:filtered={(e) => {
					setExcluded(e.detail.dataIndex, e.detail.newValue);
				}}
				{data}
			/>
			{#if data.length === maxDataCount}
				<div class="align-middle w-full text-center pb-[8rem]" use:loadMoreTrigger>
					Search a gem name for more...
				</div>
			{/if}
		</div>
	</div>
{/if}
