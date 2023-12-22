<script lang="ts">
	import type { ActionData, PageData } from './$types';
	import AnimatedSearchButton from '$lib/client/AnimatedSearchButton.svelte';
	import * as hi from '@steeze-ui/heroicons';
	import { Icon } from '@steeze-ui/svelte-icon';
	import { Wrapper, WrapperItem } from '$lib/client/wrapper';
	import { localSettings } from '$lib/client/localSettings';
	import { superForm } from 'sveltekit-superforms/client';
	import { loadoutRequestSchema } from '$lib/loadout';

	export let data: PageData;
	export let form: ActionData;
	const {
		form: loadoutForm,
		capture,
		restore,
		errors,
		constraints,
		enhance,
		delayed
	} = superForm(data.loadoutForm, {
		validators: loadoutRequestSchema,
		taintedMessage: null
	});

	export const snapshot = { capture, restore };
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
			<input type="hidden" name="league" value={$localSettings.league} />
			<label class="label">
				<span>Red Sockets</span>
				<input
					class="input"
					type="number"
					name="gems_red"
					bind:value={$loadoutForm.red}
					{...$constraints.red}
				/>
				{#if $errors.red}
					<aside class="alert variant-glass-error">{$errors.red}</aside>
				{/if}
			</label>
			<label class="label">
				<span>Green Sockets</span>
				<input
					class="input"
					type="number"
					name="gems_green"
					bind:value={$loadoutForm.green}
					{...$constraints.green}
				/>
				{#if $errors.green}
					<aside class="alert variant-glass-error">{$errors.green}</aside>
				{/if}
			</label>
			<label class="label">
				<span>Blue Sockets</span>
				<input
					class="input"
					type="number"
					name="gems_blue"
					bind:value={$loadoutForm.blue}
					{...$constraints.blue}
				/>
				{#if $errors.blue}
					<aside class="alert variant-glass-error">{$errors.blue}</aside>
				{/if}
			</label>
			<label class="label">
				<span>White Sockets</span>
				<input
					class="input"
					type="number"
					name="gems_white"
					bind:value={$loadoutForm.white}
					{...$constraints.white}
				/>
				{#if $errors.white}
					<aside class="alert variant-glass-error">{$errors.white}</aside>
				{/if}
			</label>
			<AnimatedSearchButton type="submit" class="shadow-lg text-2xl">
				<Icon src={hi.MagnifyingGlass} size="22" />
				<span class="mr-0.5">Search</span></AnimatedSearchButton
			>
		</form>
	</WrapperItem>
</Wrapper>

<style lang="postcss"></style>
