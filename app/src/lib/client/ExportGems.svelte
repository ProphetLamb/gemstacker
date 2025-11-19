<script lang="ts">
	import type { GemProfitResponse, GemProfitResponseItemPrice } from "$lib/gemLevelProfitApi";
	import { Icon } from "@steeze-ui/svelte-icon";
    import * as hi from '@steeze-ui/heroicons'

    export let data: GemProfitResponse

    $: csv = csvPayload(data)

    function csvPayload(data: GemProfitResponse) {
        return data.reduce((agg, g) => `${agg}\n${g.name},${csvPrice(g.min)},${csvPrice(g.max)},${g.preferred_recipe}`, 'name,buy_level,buy_quality,buy_corrupted,buy_price,sell_level,sell_quality,sell_corrupted,sell_price,preferred_recipe')

        function csvPrice(p: GemProfitResponseItemPrice) {
            return `${p.level},${p.quality},${p.corrupted},${p.price}`
        }
    }
</script>

<a href="data:text/csv;charset=utf-8,{encodeURIComponent(csv)}" download="gemstacker-{new Date().toLocaleDateString()}" class="btn btn-sm text-token variant-ghost-tertiary shadow" title="Download CSV">
    <Icon src={hi.ArrowDown} size=16 />
</a>