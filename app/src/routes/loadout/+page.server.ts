import { fail, type Actions } from '@sveltejs/kit';
import { API_KEY, API_ENDPOINT } from '$env/static/private';
import { createGemProfitApi } from '$lib/server/gemLevelProfitApi';
import { superValidate } from 'sveltekit-superforms/client';
import type { PageServerLoad } from './$types';
import { LoadoutOptimizer, loadoutRequestSchema, type LoadoutResponse } from '$lib/loadout';

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
      const gemProfit = await gemProfitApi.getGemProfit({ league: loadoutRequest.league, min_experience_delta: loadoutRequest.min_experience_delta, items_count: -1 });
      const loadoutOptimzer = new LoadoutOptimizer(loadoutRequest, gemProfit);
      const items = loadoutOptimzer.optimize();
      const totalBuyCost = items.map(x => x.gem.min.price * x.count).reduce((l, r) => l + r, 0)
      const totalSellPrice = items.map(x => x.gem.max.price * x.count).reduce((l, r) => l + r, 0)
      const totalExperience = items.map(x => x.gem.max.experience - x.gem.min.experience).reduce((l, r) => Math.max(l, r), 0)
      const count = items.map(x => x.count).reduce((l, r) => l + r, 0)
      return { ...response, loadout: { items, totalBuyCost, totalSellPrice, totalExperience, count } satisfies LoadoutResponse };
    } catch (error) {
      return fail(500, response);
    }
  }
};
