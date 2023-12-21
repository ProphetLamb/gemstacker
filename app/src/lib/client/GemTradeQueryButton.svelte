<script lang="ts">
	import type { GemProfitResponseItem } from '$lib/gemLevelProfitApi';
	import { createGemTradeQueryBody, type PoeTradeGemRequest } from '$lib/pathOfExileApi';

	export let gemPrice: GemProfitResponseItem;

	let buyTradeUrl = getTradeQueryUrl(
		gemPrice.type,
		gemPrice.discriminator,
		gemPrice.min.level,
		gemPrice.max.level,
		gemPrice.min.quality
	);
	let sellTradeUrl = getTradeQueryUrl(
		gemPrice.type,
		gemPrice.discriminator,
		gemPrice.max.level,
		undefined,
		gemPrice.max.quality
	);

	function getTradeQueryUrl(
		name: string,
		discriminator?: string | null,
		min_level?: number,
		max_level?: number,
		min_quality?: number
	) {
		let body = JSON.stringify(
			createGemTradeQueryBody({
				name,
				discriminator,
				corrupted: false,
				max_level,
				min_level,
				min_quality
			})
		);
		let url = `https://www.pathofexile.com/trade/search/Ancestor?q=${body}`;
		return url;
	}
</script>

<div class="btn-group variant-soft-success">
	<a class="" href={buyTradeUrl} target="_blank">Buy</a>
	<a class="variant-soft-surface" href={sellTradeUrl} target="_blank">Sell</a>
</div>
