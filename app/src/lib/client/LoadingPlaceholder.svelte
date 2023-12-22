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
	export let cols: NumberOrRange = { min: 1, max: 5 };

	function randomInt(value: NumberOrRange) {
		if (typeof value === 'number') {
			return value;
		}
		const min = value.min;
		const max = value.max + 1;
		const rng = min + Math.floor(Math.random() * (max - min));
		return rng;
	}
</script>

<section class="relative {classes}">
	<div class="p-4 space-y-4 {placeholder}">
		{#each { length: randomInt(rows) } as _, idx}
			{@const colCount = randomInt(cols)}
			{@const doubleCol = colCount <= 2 ? undefined : randomInt({ min: 0, max: colCount - 1 })}
			<div class="grid grid-cols-{colCount + (doubleCol === undefined ? 0 : 1)} gap-8 {row}">
				{#each { length: colCount } as _, idx}
					{@const colSpan = idx === doubleCol ? 'col-span-2' : ''}
					<div class="placeholder {colSpan} {cell}" />
				{/each}
			</div>
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
