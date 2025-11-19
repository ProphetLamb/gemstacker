<script lang="ts">
	import { currencyTypeDisplay, type GemProfitResponeItemMargin } from "$lib/gemLevelProfitApi";
	import Currency from "./Currency.svelte";

    export let recipe: GemProfitResponeItemMargin | undefined | null
</script>

<ul class="{$$props.class ?? ''}">
    {#each Object.entries(recipe?.recipe_cost ?? {}) as [key, cost]}
        {@const currency = currencyTypeDisplay(key)}
        <li class="flex flex-row justify-between">
            <span>{currency.name}</span>
            <Currency value={cost} alt={currency.alt} src={currency.img} />
        </li>
        <hr />
    {/each}
</ul>
{#if recipe?.min_attempts_to_profit}
    <p
        class="{recipe.min_attempts_to_profit < 3
            ? 'text-success-400-500-token'
            : 'text-warning-400-500-token'} font-semibold"
    >
        {#if recipe.min_attempts_to_profit === 1}
            Guaranteed profit with this recipe.
        {:else}
            At least {recipe.min_attempts_to_profit} attempts needed for a 66% expectation of
            significant profit.
        {/if}
    </p>
{/if}

{#if recipe?.min_attempts_to_profit === 0}
    <p class="text-error-400-500-token font-semibold">
        No profit expectation at all with this recipe.
    </p>
{/if}