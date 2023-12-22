<script>
	import { enhance } from '$app/forms';
	import * as hi from '@steeze-ui/heroicons';
	import { Icon } from '@steeze-ui/svelte-icon';
</script>

<div class="w-full h-full mx-auto flex flex-wrap gap-4 justify-center items-start">
	<article class="space-y-10 text-center flex flex-col items-center p-4">
		<h1 class="h1">
			Your <span
				class="bg-clip-text shadow-surface-500 text-transparent bg-gradient-to-tr from-indigo-500 to-sky-300 via-accent animate-gradient-xy font-bold"
				>loadout</span
			> your gems.
		</h1>
		<form class="space-y-2" use:enhance method="POST">
			<h2 class="h2">Sockets</h2>
			<label class="label">
				<span>Red</span>
				<input class="input" type="number" name="gems_red" />
			</label>
			<label class="label">
				<span>Green</span>
				<input class="input" type="number" name="gems_green" />
			</label>
			<label class="label">
				<span>Blue</span>
				<input class="input" type="number" name="gems_blue" />
			</label>
			<label class="label">
				<span>White</span>
				<input class="input" type="number" name="gems_white" />
			</label>
			<button
				type="submit"
				class="shadow-lg btn text-token variant-filled bg-gradient-to-br from-indigo-700 to-fuchsia-800 shadow-fuchsia-900/50 after:bg-gradient-to-br after:from-orange-800 after:to-amber-400 hover:shadow-orange-400/50 via-accent animate-gradient-x after:animate-gradient-x text-2xl transition-all duration-[1s]"
			>
				<Icon src={hi.MagnifyingGlass} size="22" />
				<span class="mr-0.5">Search</span></button
			>
		</form>
	</article>
</div>

<style lang="postcss">
	button[type='submit'] {
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
			height: 150%;
			width: 200%;
			transform: translate(-50%, 100%);
			transform-origin: top;
			transition: 1s transform ease;
			will-change: transform;
			z-index: -1;
		}
		&:hover::after {
			transform: translate(0, 0);
		}
	}
</style>
