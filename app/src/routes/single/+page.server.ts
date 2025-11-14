import { fail, type Actions } from '@sveltejs/kit';
import { API_KEY, API_ENDPOINT } from '$env/static/private';
import { createGemProfitApi } from '$lib/server/gemLevelProfitApi';
import { superValidate } from 'sveltekit-superforms/client';
import { gemProfitRequestParameterSchema } from '$lib/gemLevelProfitApi';
import type { PageServerLoad } from './$types';
import { setFlash } from 'sveltekit-flash-message/server';
import type { ToastMessage } from '$lib/toast';

export const load: PageServerLoad = async ({ request }) => {
	const gemLevelsProfitForm = await superValidate(request, gemProfitRequestParameterSchema);
	return { gemLevelsProfitForm };
};

export const actions: Actions = {
	default: async (event) => {
		const { request, fetch } = event
		const gemLevelsProfitForm = await superValidate(request, gemProfitRequestParameterSchema);
		const response = { gemLevelsProfitForm }

		if (!gemLevelsProfitForm.valid) {
			setFlash({ message: 'Please enter valid data', type: 'error' } satisfies ToastMessage, event)
			return fail(400, response);
		}

		const gemProfitApi = createGemProfitApi(fetch, {
			api_endpoint: API_ENDPOINT,
			api_key: API_KEY
		});

		try {
			const gemProfit = await gemProfitApi.getGemProfit(gemLevelsProfitForm.data);
			return { ...response, gemProfit };
		} catch (err) {
			console.log('/single:actions.default', err)
			const message = err instanceof Error ? `Oooops... something went wrong: ${err.message}` : "Oooops... something's really fucked";
			setFlash({ message, type: 'error' } satisfies ToastMessage, event)
			return fail(500, response);
		}
	}
};
