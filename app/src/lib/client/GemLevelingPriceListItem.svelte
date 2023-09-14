<script lang="ts">
	import type { GemProfitResponseItem } from '$lib/gemLevelProfitApi';

	export let data: GemProfitResponseItem;
	export let idx: number;
	export let name: string;
</script>

<span class="badge-icon p-4 variant-soft-primary">{idx + 1}.</span>
<span class="flex-auto">{name}</span>
<span>{data.min.price}c @ lvl{data.min.level}</span>
<span>{data.max.price}c @ lvl{data.max.level}</span>
<button class="btn variant-soft-primary">Trade</button>
