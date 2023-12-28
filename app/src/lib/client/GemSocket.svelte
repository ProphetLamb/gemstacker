<script lang="ts">
	import type { GemColor } from '$lib/gemLevelProfitApi.ts';
	import type { CssClasses } from '@skeletonlabs/skeleton';

	export let color: GemColor = 'white';
	let classes: CssClasses = '';
	export { classes as class };
</script>

<div class="rounded-full border-2 gem-{color} {classes}">
	<slot />
</div>

<style lang="postcss">
	.gem-red {
		@apply border-red-500 bg-red-700/40 shadow-red-500/50;
	}
	.gem-green {
		@apply border-green-500 bg-green-700/40 shadow-green-500/50;
	}
	.gem-blue {
		@apply border-blue-500 bg-blue-700/40 shadow-blue-500/50;
	}
	.gem-white {
		@apply border-white bg-gray-400/40 shadow-white/50;
	}
</style>
