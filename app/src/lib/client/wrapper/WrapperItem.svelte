<script lang="ts">
	import type { CssClasses } from '@skeletonlabs/skeleton';
	let classes: CssClasses = '';
	export { classes as class };
</script>

<article class="space-y-10 text-center flex flex-col items-center p-4 {classes}">
	<slot />
</article>
