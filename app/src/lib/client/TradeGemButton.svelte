<script lang="ts">
	import {
		PathofExileApi,
		type PoeTradeGemRequest,
		type PoeTradeLeagueRequest
	} from '$lib/pathOfExileApi';
	import { ProgressRadial } from '@skeletonlabs/skeleton';

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
		<button class="btn variant-soft-secondary" disabled>
			<ProgressRadial font={12} width="w-8" />
		</button>
	{:then trade_url}
		<a class="btn variant-soft-secondary" href={trade_url}>Open</a>
	{:catch error}
		<button class="btn variant-soft-secondary" on:click={startGenerateTradeUrl}>Failed</button>
	{/await}
{:else}
	<button class="btn variant-soft-secondary" on:click={startGenerateTradeUrl}>Trade</button>
{/if}
