<script lang="ts">
	import {
		PathofExileApi,
		type PoeTradeGemRequest,
		type PoeTradeLeagueRequest
	} from '$lib/pathOfExileApi';

	export let name: string;
	export let min_level: number | undefined;
	export let max_level: number | undefined;
	export let corrupted: boolean | undefined;
	let trade_url: Promise<string>;
	function startGenerateTradeUrl() {
		trade_url = (async () => {
			const api = new PathofExileApi(fetch, {});
			const league = { realm: 'pc', trade_league: 'Softcore' } satisfies PoeTradeLeagueRequest;
			const trade = {
				type: 'gem',
				min_level,
				max_level,
				name,
				corrupted
			} satisfies PoeTradeGemRequest;
			const tradeQuery = await api.createTradeQuery({ ...trade, ...league });
			const tradeUrl = tradeQuery.web_trade_url;
			return tradeUrl;
		})();
	}
</script>

{#if trade_url}
	{#await trade_url}
		<button class="button" disabled>
			<div class="spinner" />
			Generating...
		</button>
	{:then trade_url}
		<a class="button" href={trade_url}>Open trade</a>
	{/await}
{:else}
	<button class="button" on:click={startGenerateTradeUrl}> Trade Gem </button>
{/if}
