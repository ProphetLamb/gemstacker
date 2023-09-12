import { fail } from '@sveltejs/kit';
import { API_KEY, API_ENDPOINT } from '$env/static/private';
import { GemProfitApi } from '$lib/server/gemLevelProfitApi';
import { z } from 'zod';
import { superValidate } from 'sveltekit-superforms/dist/superValidate';
import type { PageServerLoad } from './$types';

const schema = z.object({
	gem_name: z.string(),
	min_sell_price_chaos: z.number().optional(),
	max_buy_price_chaos: z.number().optional(),
	min_experience_delta: z.number().min(1000000).max(100000000),
	items_offset: z.number().optional().default(0),
	items_count: z.number().optional().default(10)
});

export const load: PageServerLoad = async ({ request, fetch }) => {
	const form = await superValidate(request, schema);

	if (!form.valid) {
		return fail(400, { form });
	}

	const gemProfitApi = new GemProfitApi(fetch, {
		api_endpoint: API_ENDPOINT,
		api_key: API_KEY
	});

	try {
		const gemProfit = await gemProfitApi.getGemProfit(form.data);

		return { form, gemProfit: Promise.resolve(gemProfit) };
	} catch (error) {
		return fail(500, { form, error });
	}
};
