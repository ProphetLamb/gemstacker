<script lang="ts">
	import type { GemProfitResponseItem } from '$lib/gemLevelProfitApi';
	import { createGemTradeQueryBody, type PoeTradeGemRequest } from '$lib/pathOfExileApi';

	export let gemPrice: GemProfitResponseItem;

	let web_trade_url = getTradeQueryUrl();

	function getTradeQueryUrl() {
		let body = JSON.stringify(
			createGemTradeQueryBody({
				name: gemPrice.name,
				corrupted: false,
				max_level: gemPrice.max.level,
				min_level: gemPrice.min.level
			})
		);
		let url = `https://www.pathofexile.com/trade/search/Ancestor?q=${body}`;
		return url;
	}
</script>

<a class="btn variant-soft-success w-[5rem]" href={web_trade_url} target="_blank">Buy</a>
