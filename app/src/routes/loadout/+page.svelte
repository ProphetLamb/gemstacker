<script>
	import { enhance } from '$app/forms';
	import AnimatedSearchButton from '$lib/client/AnimatedSearchButton.svelte';
	import * as hi from '@steeze-ui/heroicons';
	import { Icon } from '@steeze-ui/svelte-icon';
	import { Wrapper, WrapperItem } from '$lib/client/wrapper';
</script>

<Wrapper>
	<WrapperItem>
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
	</WrapperItem>
</Wrapper>

<style lang="postcss"></style>
