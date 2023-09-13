import { fail, type Actions } from '@sveltejs/kit';
import { API_KEY, API_ENDPOINT } from '$env/static/private';
import { GemProfitApi } from '$lib/server/gemLevelProfitApi';
import type { PageServerLoad } from './$types';
import { superValidate } from 'sveltekit-superforms/client';
import { gemLevelsProfitSchema } from './page.schema';

export const load: PageServerLoad = async ({ request }) => {
	const form = await superValidate(request, gemLevelsProfitSchema);
	return { form };
};

export const actions: Actions = {
	default: async ({ request }) => {
		const form = await superValidate(request, gemLevelsProfitSchema);
		console.log(form);
		if (!form.valid) {
			return fail(400, { form });
		}

		const gemProfitApi = new GemProfitApi(fetch, {
			api_endpoint: API_ENDPOINT,
			api_key: API_KEY
		});

		try {
			const gemProfit = await gemProfitApi.getGemProfit(form.data);

			return { form, gemProfit };
		} catch (error) {
			return fail(500, { form });
		}
	}
};
