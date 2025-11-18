<script lang="ts">
	import type { GemProfitResponseItem } from '$lib/gemLevelProfitApi';
	import { createGemTradeQueryBody, type PoeTradeGemRequest } from '$lib/pathOfExileApi';
	import { localSettings } from './localSettings';

	export let gemPrice: GemProfitResponseItem;

	let buyTradeUrl = getTradeQueryUrl({
		name: gemPrice.type,
		discriminator: gemPrice.discriminator,
		corrupted: gemPrice.min.corrupted,
		min_level: gemPrice.min.level,
		max_level: gemPrice.max.level,
		min_quality: gemPrice.min.quality,
	});
	let sellTradeUrl = getTradeQueryUrl({
		name: gemPrice.type,
		discriminator: gemPrice.discriminator,
		corrupted: gemPrice.max.corrupted,
		min_level: gemPrice.max.level,
		min_quality: gemPrice.max.quality,
	}
	);

	function getTradeQueryUrl(query: PoeTradeGemRequest) {
		let body = JSON.stringify(
			createGemTradeQueryBody(query)
		);
		let url = `https://www.pathofexile.com/trade/search/${$localSettings.league}?q=${body}`;
		return url;
	}
</script>

<div class="btn-group variant-soft-success flex-col rounded-xl md:flex-row md:rounded-full">
	<a href={buyTradeUrl} target="_blank">Buy</a>
	<a class="variant-soft-surface" href={sellTradeUrl} target="_blank">Sell</a>
</div>
