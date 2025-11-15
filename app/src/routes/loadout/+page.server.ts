import { fail, type Actions } from '@sveltejs/kit';
import { API_KEY, API_ENDPOINT } from '$env/static/private';
import { createGemProfitApi } from '$lib/server/gemLevelProfitApi';
import { superValidate } from 'sveltekit-superforms/client';
import type { PageServerLoad } from './$types';
import { loadoutRequestSchema } from '$lib/loadout';
import { setFlash } from 'sveltekit-flash-message/server';
import type { ToastMessage } from '$lib/toast';

export const load: PageServerLoad = async ({ request }) => {
	const loadoutForm = await superValidate(request, loadoutRequestSchema);
	return { loadoutForm };
};

export const actions: Actions = {
	default: async (event) => {
		const { request, fetch } = event;
		const loadoutForm = await superValidate(request, loadoutRequestSchema);
		const response = { loadoutForm };

		if (!loadoutForm.valid) {
			setFlash({ message: 'Please enter valid data', type: 'error' } satisfies ToastMessage, event);
			return fail(400, response);
		}

		const gemProfitApi = createGemProfitApi(fetch, {
			api_endpoint: API_ENDPOINT,
			api_key: API_KEY
		});

		const loadoutRequest = ({
      league: loadoutForm.data.league,
      min_experience_delta: loadoutForm.data.min_experience_delta,
      min_listing_count: loadoutForm.data.min_listing_count,
      items_count: -1
    });
		try {
			return {
				...response,
				gem_profit: await gemProfitApi.getGemProfit(loadoutRequest),
				exchange_rates: await gemProfitApi.getWellKnownExchangeRatesToChaos(loadoutRequest.league),
			};
		} catch (err) {
			console.log('/loadout:actions.default', err);
			const message =
				err instanceof Error
					? `Oooops... something went wrong: ${err.message}`
					: "Oooops... something's really fucked";
			setFlash({ message, type: 'error' } satisfies ToastMessage, event);
			return fail(500, response);
		}
	}
};
