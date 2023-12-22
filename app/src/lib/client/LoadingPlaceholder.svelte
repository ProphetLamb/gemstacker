<script lang="ts">
	import type { CssClasses } from '@skeletonlabs/skeleton';

	type NumberOrRange = number | { min: number; max: number };

	let classes: CssClasses = '';
	export { classes as class };
	export let placeholder: CssClasses = '';
	export let front: CssClasses = '';
	export let row: CssClasses = '';
	export let cell: CssClasses = '';
	export let rows: NumberOrRange = 4;
	export let cols: NumberOrRange = { min: 2, max: 6 };

	function randomIntUneven(value: NumberOrRange, idx?: number) {
		if (typeof value === 'number') {
			return value;
		}
		const min = value.min;
		const max = value.max;
		const rng = min + Math.round(Math.random() * (max - min));
		if (idx === undefined) {
			return rng;
		}
		const idxEven = idx % 2 == 0;
		const rngEven = rng % 2 == 0;
		if (idxEven === rngEven) {
			return rng;
		}
		const lower = rng - 1;
		if (lower >= min) {
			return lower;
		}
		const higher = rng + 1;
		if (higher <= max) {
			return higher;
		}
		return rng;
	}
</script>

<section class="relative {classes}">
	<div class="p-4 space-y-4 {placeholder}">
		{#each { length: randomIntUneven(rows) } as _, idx}
			{@const colCount = randomIntUneven(cols, idx)}
			<div class="grid grid-flow-col grid-cols-[{colCount}] gap-8 {row}">
				{#each { length: colCount } as _}
					<div class="placeholder {cell}" />
				{/each}
			</div>
		{/each}
	</div>
	<div
		class="card bg-surface-backdrop-token p-4 absolute top-[50%] left-[50%] translate-x-[-50%] translate-y-[-50%] transform flex flex-col items-center shadow-xl {front}"
	>
		<slot />
	</div>
</section>
