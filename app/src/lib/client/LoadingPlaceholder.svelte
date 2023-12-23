<script lang="ts">
	import { createRng } from '$lib/rng';
	import type { CssClasses } from '@skeletonlabs/skeleton';

	let classes: CssClasses = '';
	export { classes as class };
	export let placeholder: CssClasses = '';
	export let front: CssClasses = '';
	export let rows: number = 10;
	export let seed: string = 'poe-gemleveling-profit-calculator-local-settings';

	const rng = createRng(seed);

	function getRow() {
		let sizes = [];
		let cols = 0;
		while (cols < 8) {
			if (checkedAdd(4, 0.7)) {
				continue;
			}
			if (checkedAdd(3, 0.7)) {
				continue;
			}
			if (checkedAdd(2, 0.7)) {
				continue;
			}
			if (checkedAdd(1, 0.7)) {
				continue;
			}
			sizes.push(0);
			cols += 1;
		}
		sizes = sizes.sort((a, b) => 0.5 - rng());
		return sizes;

		function checkedAdd(size: number, probability: number) {
			if (cols + size < 8 && rng() <= probability) {
				sizes.push(size);
				cols += size;
				return true;
			}
			return false;
		}
	}
</script>

<section class="relative {classes}">
	<div class="grid grid-cols-8 p-4 gap-4 {placeholder}">
		{#each { length: rows } as _}
			{#each getRow() as width}
				{#if width === 1}
					<div class="placeholder" />
				{:else if width === 2}
					<div class="placeholder col-span-2" />
				{:else if width === 3}
					<div class="placeholder col-span-3" />
				{:else if width === 4}
					<div class="placeholder col-span-4" />
				{:else}
					<div class="opacity-0" />
				{/if}
			{/each}
		{/each}
	</div>
	<div
		class="card p-4 absolute top-[50%] left-[50%] translate-x-[-50%] translate-y-[-50%] transform flex flex-col items-center shadow-xl {front}"
	>
		<slot />
	</div>
</section>

<style lang="postcss">
	div.gird {
	}
</style>
