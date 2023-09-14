import { fail, type Actions } from '@sveltejs/kit';
import { API_KEY, API_ENDPOINT } from '$env/static/private';
import { GemProfitApi } from '$lib/gemLevelProfitApi';
import type { PageServerLoad } from './$types';
import { superValidate } from 'sveltekit-superforms/client';
import { gemLevelsProfitSchema } from '$lib/gemLevelProfitApi';

export const load: PageServerLoad = async ({ request }) => {
	const gemLevelsProfitForm = await superValidate(request, gemLevelsProfitSchema);
	return { gemLevelsProfitForm };
};

export const actions: Actions = {
	default: async () => {
	},
	getGemLevelProfit: async ({ request }) => {
		const gemLevelsProfitForm = await superValidate(request, gemLevelsProfitSchema);
		console.log(gemLevelsProfitForm);
		if (!gemLevelsProfitForm.valid) {
			return fail(400, { gemLevelsProfitForm });
		}

		const gemProfitApi = new GemProfitApi(fetch, {
			api_endpoint: API_ENDPOINT,
			api_key: API_KEY
		});

		try {
			const gemProfit = await gemProfitApi.getGemProfit(gemLevelsProfitForm.data);

			return { gemLevelsProfitForm, gemProfit };
		} catch (error) {
			return fail(500, { gemLevelsProfitForm });
		}
	}
};
