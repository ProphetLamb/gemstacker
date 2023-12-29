<script lang="ts">
	import type { CssClasses } from '@skeletonlabs/skeleton';
	let classes: CssClasses = '';
	export { classes as class };
</script>

<div class="w-full mx-auto flex flex-wrap gap-4 justify-center items-start {classes}">
	<slot />
</div>
