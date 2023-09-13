import type { Actions } from '@sveltejs/kit';
import { API_KEY, API_ENDPOINT } from '$env/static/private';
import { GemProfitApi } from '$lib/server/gemLevelProfitApi';
import { z } from 'zod';
import type { PageServerLoad } from './$types';
import { superValidate } from 'sveltekit-superforms/client';

const schema = z.object({
	gem_name: z.string().optional(),
	min_sell_price_chaos: z.number().optional(),
	max_buy_price_chaos: z.number().optional(),
	min_experience_delta: z.number().min(1000000).max(100000000).optional().default(10000000),
	items_offset: z.number().optional().default(0),
	items_count: z.number().optional().default(10)
});

export const load: PageServerLoad = async () => {
	const form = await superValidate(schema);
	return { form }
};

export const actions: Actions = {
	default: async ({ fetch, request }) => {
		const form = await superValidate(request, schema);

		if (!form.valid) {
			return { form };
		}

		const gemProfitApi = new GemProfitApi(fetch, {
			api_endpoint: API_ENDPOINT,
			api_key: API_KEY
		});

		try {
			const gemProfit = await gemProfitApi.getGemProfit(form.data);

			return { form, gemProfit };
		} catch (error) {
			return { form };
		}
	}
};
