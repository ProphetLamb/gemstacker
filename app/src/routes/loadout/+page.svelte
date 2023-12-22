<script>
	import { enhance } from '$app/forms';
	import AnimatedSearchButton from '$lib/client/AnimatedSearchButton.svelte';
	import * as hi from '@steeze-ui/heroicons';
	import { Icon } from '@steeze-ui/svelte-icon';
</script>

<div class="w-full h-full mx-auto flex flex-wrap gap-4 justify-center items-start">
	<article class="space-y-10 text-center flex flex-col items-center p-4">
		<h1 class="h1">
			Your <span
				class="bg-clip-text shadow-surface-500 text-transparent bg-gradient-to-tr from-indigo-500 to-sky-300 via-accent animate-gradient-xy font-bold"
				>loadout</span
			>, your gems.
		</h1>
		<form class="space-y-2" use:enhance method="POST">
			<label class="label">
				<span>Red Sockets</span>
				<input class="input" type="number" name="gems_red" />
			</label>
			<label class="label">
				<span>Green Sockets</span>
				<input class="input" type="number" name="gems_green" />
			</label>
			<label class="label">
				<span>Blue Sockets</span>
				<input class="input" type="number" name="gems_blue" />
			</label>
			<label class="label">
				<span>White Sockets</span>
				<input class="input" type="number" name="gems_white" />
			</label>
			<AnimatedSearchButton type="submit" class="shadow-lg text-2xl">
				<Icon src={hi.MagnifyingGlass} size="22" />
				<span class="mr-0.5">Search</span></AnimatedSearchButton
			>
		</form>
	</article>
</div>

<style lang="postcss"></style>
