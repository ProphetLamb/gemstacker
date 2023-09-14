import { PathofExileApi } from '$lib/server/pathOfExileApi';
import type { RequestHandler } from '@sveltejs/kit';

export const POST: RequestHandler = async ({ fetch, request }) => {
	const rawParam = request.json();
	const param = GemProfitRequestParameterSchema.;

	const api = new PathofExileApi(fetch, {});
	await api.createTradeQuery();
};
