<script lang="ts">
	import { localSettings, type LocalSettings } from './localSettings';
	import { onMount } from 'svelte';
	import { wellKnownRecipeInfo } from '$lib/recipe';

	$: probabilisitcRecipes = undefined as boolean | undefined;

	function getProbabilisticRecipes(localSettings: LocalSettings) {
		return !Object.entries(wellKnownRecipeInfo).every(
			([name, info]) => !info.probabilistic || localSettings.disallowed_recipes.includes(name)
		);
	}

	function setProbabilisticRecipes(probabilisitcRecipes?: boolean) {
		if (probabilisitcRecipes === null || probabilisitcRecipes === undefined) {
			return;
		}
		const disallowedRecipes = new Set($localSettings.disallowed_recipes);
		for (const [name, info] of Object.entries(wellKnownRecipeInfo)) {
			if (!info.probabilistic) {
				continue;
			}
			if (probabilisitcRecipes) {
				disallowedRecipes.delete(name);
			} else {
				disallowedRecipes.add(name);
			}
		}
		$localSettings.disallowed_recipes = [...disallowedRecipes];
	}

	onMount(() => {
		probabilisitcRecipes = getProbabilisticRecipes($localSettings);
	});
</script>

<label class="label flex items-center space-x-2">
	<input
		name="probabilistic_recipes"
		class="checkbox"
		type="checkbox"
		bind:checked={probabilisitcRecipes}
		on:change={() => setProbabilisticRecipes(probabilisitcRecipes)}
	/>
	<p>Enable probabilisitc recipes</p>
</label>
