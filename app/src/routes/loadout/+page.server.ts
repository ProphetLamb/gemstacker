import { fail, type Actions } from '@sveltejs/kit';
import { API_KEY, API_ENDPOINT } from '$env/static/private';
import { createGemProfitApi } from '$lib/server/gemLevelProfitApi';
import { superValidate } from 'sveltekit-superforms/client';
import type { PageServerLoad } from './$types';
import { computeBestLoadout as calculateOptimalLoadout, loadoutRequestSchema } from '$lib/loadout';

export const load: PageServerLoad = async ({ request }) => {
  const loadoutForm = await superValidate(request, loadoutRequestSchema);
  return { loadoutForm };
};

export const actions: Actions = {
  default: async ({ request, fetch }) => {
    const loadoutForm = await superValidate(request, loadoutRequestSchema);
    let response = { loadoutForm }

    if (!loadoutForm.valid) {
      return fail(400, response);
    }

    const gemProfitApi = createGemProfitApi(fetch, {
      api_endpoint: API_ENDPOINT,
      api_key: API_KEY
    });

    const loadoutRequest = loadoutForm.data;
    try {
      const gemProfit = await gemProfitApi.getGemProfit({ league: loadoutRequest.league, min_experience_delta: 340000000 });
      const bestLoadout = calculateOptimalLoadout(gemProfit, loadoutRequest)
      return { ...response, bestLoadout };
    } catch (error) {
      return fail(500, response);
    }
  }
};
