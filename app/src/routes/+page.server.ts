import { fail, type Actions } from '@sveltejs/kit';
import { API_KEY, API_ENDPOINT } from '$env/static/private';
import { GemProfitApi } from '$lib/server/gemLevelProfitApi';
import { z } from 'zod';
import { superValidate } from 'sveltekit-superforms/dist/superValidate';

const schema = z.object({
	gem_name: z.string(),
	min_sell_price_chaos: z.number().optional(),
	max_buy_price_chaos: z.number().optional(),
	min_experience_delta: z.number()
});

export const actions: Actions = {
	default: async () => {},
	gemProfit: async ({ request, fetch }) => {
		const form = await superValidate(request, schema);

		if (!form.valid) {
			return fail(400, { form });
		}

		const gemProfitApi = new GemProfitApi(fetch, {
			api_endpoint: API_ENDPOINT,
			api_key: API_KEY
		});

		try {
			const gemProfit = await gemProfitApi.getGemProfit({
				gem_name: '*',
				min_sell_price_chaos: 0,
				max_buy_price_chaos: 0,
				min_experience_delta: 0,
				items_offset: 0,
				items_count: 0
			});

			return { form, gemProfit };
		} catch (error) {
			return fail(500, { form, error });
		}
	}
};
