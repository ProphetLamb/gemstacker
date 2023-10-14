<script lang="ts">
	import type { GemProfitResponseItem } from '$lib/gemLevelProfitApi';
	import { createGemTradeQueryBody, type PoeTradeGemRequest } from '$lib/pathOfExileApi';
	import { ProgressRadial } from '@skeletonlabs/skeleton';

	export let data: GemProfitResponseItem;
	export let name: string;

	let web_trade_url: Promise<string | undefined> = Promise.resolve(undefined);

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

	function startSetTradeDecoupledQuery() {
		web_trade_url = Promise.resolve(getTradeQueryUrl());
	}
</script>

{#await web_trade_url}
	<button class="btn variant-soft-primary w-[5rem]" disabled>
		<ProgressRadial width="w-6" font={10} />
	</button>
{:then web_trade_url}
	{#if web_trade_url}
		<a class="btn variant-soft-success w-[5rem]" href={web_trade_url} target="_blank">Buy</a>
	{:else}
		<button class="btn variant-soft-primary w-[5rem]" on:click={startSetTradeDecoupledQuery}
			>Trade</button
		>
	{/if}
{:catch error}
	<button class="btn variant-soft-error w-[5rem]">Failed</button>
{/await}
