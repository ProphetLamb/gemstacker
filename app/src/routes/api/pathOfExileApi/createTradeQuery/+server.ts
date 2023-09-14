import { poeTradeGemRequestSchema, type PoeTradeLeagueRequest } from '$lib/pathOfExileApi';
import { PathofExileApi } from '$lib/server/pathOfExileApi';
import { error, json, type RequestHandler } from '@sveltejs/kit';

export const POST: RequestHandler = async ({ fetch, request }) => {
	const requestBody = request.json();
	const tradeQuery = poeTradeGemRequestSchema.safeParse(requestBody);
	if (!tradeQuery.success) {
		throw error(400, { message: tradeQuery.error.message });
	}
	const league = { realm: 'pc', trade_league: 'Softcore' } satisfies PoeTradeLeagueRequest;
	const api = new PathofExileApi(fetch, {});
	try {
		const result = await api.createTradeQuery({ type: 'gem', ...league, ...tradeQuery.data });
		return json({ url: result.web_trade_url });
	} catch (e) {
		if (e instanceof Error) {
			throw error(500, { message: e.message });
		}
		throw error(500, { message: 'Something went wrong' });
	}
};
