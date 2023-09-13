<script lang="ts">
	import { enhance } from '$app/forms';
	import { superForm } from 'sveltekit-superforms/client';
	import type { ActionData, PageData } from './$types';
	export let form: ActionData;
	export let data: PageData;
	const gemProfit = form?.gemProfit;
	const { form: formData, errors, constraints } = superForm(form?.form ?? data.form!);
</script>

<div class="container h-full mx-auto flex justify-center items-center">
	<div class="space-y-10 text-center flex flex-col items-center">
		<h2 class="h2">Gem levels for profit.</h2>
		<form class="space-y-2" use:enhance method="POST">
			<label class="label">
				<span>Gem Name</span>
				<input class="input" type="text" bind:value={$formData.gem_name} {...$constraints.gem_name} />
				{#if $errors.gem_name}
					<aside class="alert variant-glass-error">{$errors.gem_name}</aside>
				{/if}
			</label>
			<label class="label">
				<span>Sell price (minimum Chaos value)</span>
				<input
					class="input"
					type="number"
					bind:value={$formData.min_sell_price_chaos}
					{...$constraints.min_sell_price_chaos}
				/>
				{#if $errors.min_sell_price_chaos}
					<aside class="alert variant-glass-error">{$errors.min_sell_price_chaos}</aside>
				{/if}
			</label>
			<label class="label">
				<span>Buy price (maximum Chaos value)</span>
				<input
					class="input"
					type="number"
					bind:value={$formData.max_buy_price_chaos}
					{...$constraints.max_buy_price_chaos}
				/>
				{#if $errors.max_buy_price_chaos}
					<aside class="alert variant-glass-error">{$errors.max_buy_price_chaos}</aside>
				{/if}
			</label>
			<label class="label">
				<span>Minimum experience required for leveling</span>
				<input
					class="input"
					type="range"
					min={1000000}
					max={100000000}
					step={1000000}
					bind:value={$formData.min_experience_delta}
					{...$constraints.min_experience_delta}
				/>
				<span>{$formData.min_experience_delta}exp</span>
				{#if $errors.min_experience_delta}
					<aside class="alert variant-glass-error">{$errors.min_experience_delta}</aside>
				{/if}
			</label>
			<button class="btn variant-filled" type="submit">
				Search
			</button>
			<hr class="w-full pb-12" />
			<h3 class="h3">The best gems for you.</h3>
			<div class="text-token card p-4 space-y-4">
				{#if gemProfit}
				<ol class="list">
					{#each Object.entries(gemProfit.data) as [gemName, gemProfitData], idx}
						<li>
							<span class="badge-icon p-4 variant-soft-primary">{idx + 1}.</span>
							<span class="flex-auto">{gemName}</span>
							<span>{gemProfitData.min.price}c @ lvl{gemProfitData.min.level}</span>
							<span>{gemProfitData.max.price}c @ lvl{gemProfitData.max.level}</span>
						</li>
					{/each}
				</ol>
				{:else}
				<p>Enter your search criteria above.</p>
				{/if}
			</div>
			<hr class="w-full pb-12" />
		</form>
	</div>
</div>
