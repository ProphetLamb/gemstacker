<script lang="ts">
	import type { CssClasses } from '@skeletonlabs/skeleton';
	let classes: CssClasses | undefined = undefined;
	export { classes as class };
</script>

<div class="w-full h-full mx-auto flex flex-wrap gap-4 justify-center items-start {classes}">
	<slot />
</div>
