<script lang="ts">
	import type { GemProfitResponseItem } from '$lib/gemLevelProfitApi';
	import { createGemTradeQueryBody, type PoeTradeGemRequest } from '$lib/pathOfExileApi';
	import { ProgressRadial } from '@skeletonlabs/skeleton';

	export let data: GemProfitResponseItem;
	export let name: string;

	let web_trade_url = getTradeQueryUrl();

	function getTradeQueryUrl() {
		let body = JSON.stringify(
			createGemTradeQueryBody({
				name,
				corrupted: false,
				max_level: data.max.level,
				min_level: data.min.level
			})
		);
		let url = `https://www.pathofexile.com/trade/search/Ancestor?q=${body}`;
		return url;
	}
</script>

<a class="btn variant-soft-success w-[5rem]" href={web_trade_url} target="_blank">Buy</a>
