import { poeTradeGemRequestSchema, type PoeTradeLeagueRequest } from '$lib/pathOfExileApi';
import { createPathOfExileApi } from '$lib/server/pathOfExileApi';
import { error, json, type RequestHandler } from '@sveltejs/kit';

export const POST: RequestHandler = async ({ fetch, request, setHeaders }) => {
	const requestBody = await request.json();
	const tradeQuery = poeTradeGemRequestSchema.safeParse(requestBody);
	if (!tradeQuery.success) {
		throw error(400, { message: tradeQuery.error.message });
	}
	const league = { realm: 'pc', trade_league: 'Softcore' } satisfies PoeTradeLeagueRequest;
	const api = createPathOfExileApi(fetch);
	try {
		const result = await api.createTradeQuery({ type: 'gem', ...league, ...tradeQuery.data });
		setHeaders({ 'Cache-Control': 'public, max-age=2592000' });
		return json(result);
	} catch (e) {
		if (e instanceof Error) {
			throw error(500, { message: e.message });
		}
		throw error(500, { message: 'Something went wrong' });
	}
};
