import { API_ENDPOINT, API_KEY } from '$env/static/private';
import {
	gemProfitRequestParameterSchema,
	type ExchangeRateToChaosResponse,
	type GemProfitResponse
} from '$lib/gemLevelProfitApi';
import { createGemProfitApi } from '$lib/server/gemLevelProfitApi';
import { error, json } from '@sveltejs/kit';

export type ProfitPreviewResponse = {
	gem_profit: GemProfitResponse;
	exchange_rates: ExchangeRateToChaosResponse;
};

export const GET = async ({ fetch, url }) => {
	const request = gemProfitRequestParameterSchema.parse({
		league: url.searchParams.get('league'),
		min_experience_delta: parseInt(url.searchParams.get('min_experience_delta') || '0')
	});
	const gemProfitApi = createGemProfitApi(fetch, {
		api_endpoint: API_ENDPOINT,
		api_key: API_KEY
	});

	try {
		const exchangeRatesPromise = gemProfitApi.getExchangeRateToChaos({
			league: request.league,
			currency: ["Cartographer's Chisel", 'Divine Orb', 'Vaal Orb']
		});
		return json(
			{
				gem_profit: await gemProfitApi.getGemProfit(request),
				exchange_rates: await exchangeRatesPromise
			} satisfies ProfitPreviewResponse,
			{
				headers: {
					'Cache-Control': 'public, immutable, no-transform, max-age=1800'
				}
			}
		);
	} catch (err) {
		console.log('/api/profit-preview.GET', err);
		const message =
			err instanceof Error
				? `Oooops... something went wrong: ${err.message}`
				: "Oooops... something's really fucked";
		error(500, message);
	}
};
