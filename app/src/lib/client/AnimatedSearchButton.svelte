<script lang="ts">
	import type { CssClasses } from '@skeletonlabs/skeleton';

	export let type: 'button' | 'submit' | 'reset' | null | undefined = undefined;
	let classes: CssClasses = '';
	export { classes as class };
</script>

<button
	{type}
	class="btn text-token variant-filled bg-gradient-to-br from-indigo-700 to-fuchsia-800 shadow-fuchsia-900/50 after:bg-gradient-to-br after:from-orange-800 after:to-amber-400 hover:shadow-orange-400/50 via-accent animate-gradient-x after:animate-gradient-x {classes}"
>
	<slot /></button
>

<style lang="postcss">
	button {
		transition-duration: 2s;
		transition-property: all;
		transition-timing-function: cubic-bezier(0.4, 0, 0.2, 1);
		animation-duration: 2s;
		position: relative;
		overflow: hidden;
		z-index: 0;
		&:after {
			animation-duration: 2s;
			clip-path: path(
				'M0,137.087 L300,137.087 L300,8.754 C300,8.754 283.833,13.254 271.833,13.254 C244.833,13.254 224.167,0 192.5,0 C153,0 128.167,14.587 100.5,14.587 C80.167,14.587 64.5,1.337 41.75,1.337 C25,1.337 12,4.92 0,9.087 L0,137.087 Z'
			);
			content: '';
			position: absolute;
			top: -50%;
			left: 0;
			height: 155%;
			width: 200%;
			transform: translate(-50%, 100%);
			transform-origin: top;
			transition: 0.7s transform ease;
			will-change: transform;
			z-index: -1;
		}
		&:hover::after {
			transform: translate(0, 0);
		}
	}
</style>
