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
		@apply border-red-500 bg-red-700 text-dark-token;
	}
	.gem-green {
		@apply border-green-500 bg-green-700 text-dark-token;
	}
	.gem-blue {
		@apply border-blue-500 bg-blue-700 text-dark-token;
	}
	.gem-white {
		@apply border-white bg-gray-400 text-token;
	}
</style>
