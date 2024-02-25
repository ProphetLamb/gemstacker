<script lang="ts">
	import { Wrapper, WrapperItem } from '$lib/client/wrapper';
	import { loadoutRequestSchema } from '$lib/loadout';
	import { superForm, superValidateSync } from 'sveltekit-superforms/client';
	import * as hi from '@steeze-ui/heroicons';
	import { Icon } from '@steeze-ui/svelte-icon';
	import { locationWithSearch } from '$lib/client/navigation';
	import * as pob from '$lib/pathOfBuilding';
	import { goto } from '$app/navigation';

	let pobText: string = '';
	$: pobError = '';

	const {
		form: loadoutForm,
		errors,
		constraints
	} = superForm(superValidateSync(loadoutRequestSchema), {
		validators: loadoutRequestSchema,
		taintedMessage: null
	});

	$: loadoutHref = locationWithSearch(
		{
			red: $loadoutForm.red,
			green: $loadoutForm.green,
			blue: $loadoutForm.blue,
			white: $loadoutForm.white
		},
		'/loadout'
	).pathSearchHash();

	async function gotoPob() {
		try {
			if (!pobText) {
				throw Error('Cannot be empty');
			}
			const xml = pob.deserializePob(pobText);
			const sockets = pob.availableSockets(xml);
			const url = locationWithSearch(sockets, '/loadout').pathSearchHash();
			goto(url);
		} catch (e) {
			if (e instanceof Error) {
				pobError = e.message;
			} else {
				pobError = 'Invalid build code';
			}
		}
	}
</script>

<div class="w-full align-middle flex justify-center p-4">
	<h1 class="h1">
		The best
		<span
			class="bg-clip-text shadow-lime-700 text-transparent bg-gradient-to-tr from-lime-700 to-yellow-600 via-accent animate-gradient-xy font-bold"
			>gems</span
		>
	</h1>
</div>
<Wrapper>
	<WrapperItem>
		<h2 class="h2">&#133;for your build</h2>
		<label class="label">
			<span>Build code</span>
			<input class="input" type="text" name="red" bind:value={pobText} />
			{#if pobError}
				<aside class="alert variant-glass-error">{pobError}</aside>
			{/if}
		</label>

		<button class="btn variant-filled-primary shadow-lg text-2xl" on:click={gotoPob}>
			<Icon src={hi.ArrowTopRightOnSquare} size="22" />
			<span class="mr-0.5">Build</span>
		</button>
	</WrapperItem>
	<div class="divider py-4 hidden sm:flex items-center flex-col">
		<div>&nbsp;</div>
		<span class="text-3xl py-2">OR</span>
		<div>&nbsp;</div>
	</div>
	<WrapperItem>
		<h2 class="h2">&#133;for your loadout</h2>
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
			<a href={loadoutHref} class="btn variant-filled-primary shadow-lg text-2xl">
				<Icon src={hi.ArrowTopRightOnSquare} size="22" />
				<span class="mr-0.5">Loadout</span>
			</a>
		</div>
	</WrapperItem>
	<div class="divider py-4 hidden sm:flex items-center flex-col">
		<div>&nbsp;</div>
		<span class="text-3xl py-2">OR</span>
		<div>&nbsp;</div>
	</div>
	<WrapperItem>
		<h2 class="h2">&#133;for your perusal</h2>
		<a href="/single" class="btn variant-filled-primary shadow-lg text-2xl">
			<Icon src={hi.ArrowTopRightOnSquare} size="22" />
			<span class="mr-0.5">Search</span>
		</a>
	</WrapperItem>
</Wrapper>

<style lang="postcss">
	div.divider {
		height: 30rem;
		padding-top: 4rem;
		& > div {
			@apply h-full w-0.5 bg-surface-200-700-token opacity-70 rounded-full;
		}
	}
</style>
