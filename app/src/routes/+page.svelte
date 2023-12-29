<script lang="ts">
	import AnimatedSearchButton from '$lib/client/AnimatedSearchButton.svelte';
	import { Wrapper, WrapperItem } from '$lib/client/wrapper';
	import { loadoutRequestSchema } from '$lib/loadout';
	import { superForm, superValidateSync } from 'sveltekit-superforms/client';
	import * as hi from '@steeze-ui/heroicons';
	import { Icon } from '@steeze-ui/svelte-icon';

	const {
		form: loadoutForm,
		errors,
		constraints
	} = superForm(superValidateSync(loadoutRequestSchema), {
		validators: loadoutRequestSchema,
		taintedMessage: null
	});
</script>

<Wrapper>
	<WrapperItem>
		<div class="space-y-2">
			<label class="label">
				<span>Red Sockets</span>
				<input
					class="input"
					type="number"
					name="red"
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
					name="green"
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
					name="blue"
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
					name="white"
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
		</div>
	</WrapperItem>
</Wrapper>
