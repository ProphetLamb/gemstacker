<script lang="ts">
	import type { GemProfitResponseItem } from '$lib/gemLevelProfitApi';
	import { createGemTradeQueryBody, type PoeTradeGemRequest } from '$lib/pathOfExileApi';
	import { localSettings } from './localSettings';

	export let gemPrice: GemProfitResponseItem;

	let buyTradeUrl = getTradeQueryUrl({
		name: gemPrice.type,
		discriminator: gemPrice.discriminator,
		corrupted: gemPrice.min.corrupted ? undefined : false,
		min_level: gemPrice.min.level,
		max_level: gemPrice.max.level,
		min_quality: gemPrice.min.quality,
	});
	let sellTradeUrl = getTradeQueryUrl({
		name: gemPrice.type,
		discriminator: gemPrice.discriminator,
		corrupted: gemPrice.max.corrupted ? undefined : false,
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
	<a href={buyTradeUrl} target="_blank" title="Buy lvl{gemPrice.min.level}/{gemPrice.min.quality}q {gemPrice.name} on trade">Buy</a>
	<a class="variant-soft-surface" href={sellTradeUrl} target="_blank" title="Sell lvl{gemPrice.max.level}/{gemPrice.max.quality}q {gemPrice.name} on trade">Sell</a>
</div>
