<script lang="ts">
	import { gemProfitResponseItemRecipeName } from '$lib/gemLevelProfitApi';
	import { getRecipeInfo } from '$lib/recipe';
	import type { EventHandler } from 'svelte/elements';

	export let excludedRecipes: string[] | null | undefined;
	export let recipesShown: number | null | undefined = undefined;
	export let onselectchanged:
		| EventHandler<MouseEvent & { excludedRecipes: string[] | null | undefined }, HTMLOptionElement>
		| null
		| undefined = undefined;
	function toggleOption(self: HTMLOptionElement) {
		self.onmousedown = (e) => {
			self.selected = !self.selected;
			if (excludedRecipes === null || excludedRecipes === undefined) {
				excludedRecipes = [];
			}
			const idx = excludedRecipes.indexOf(self.value);
			if (!self.selected) {
				if (idx >= 0) {
					excludedRecipes.splice(idx, 1);
					excludedRecipes = excludedRecipes;
				}
			} else {
				if (idx < 0) {
					excludedRecipes.push(self.value);
					excludedRecipes = excludedRecipes;
				}
			}
			if (onselectchanged) {
				onselectchanged({ ...e, currentTarget: self, excludedRecipes });
			}
			return false;
		};
	}
</script>

<select
	class="select {$$props.class}"
	multiple
	size={recipesShown === undefined ? gemProfitResponseItemRecipeName.length + 2 : recipesShown}
	bind:value={excludedRecipes}
>
	{#each gemProfitResponseItemRecipeName as name}
		{@const info = getRecipeInfo(name)}
		<option use:toggleOption value={name} selected={(excludedRecipes?.indexOf(name) ?? -1) >= 0} title={info.description}>{info.title} </option>
	{/each}
</select>
