import type { PoeTradeLeagueResponse } from "$lib/pathOfExileApi";
import { createPathOfExileApi } from "$lib/server/pathOfExileApi";
import { loadFlash } from "sveltekit-flash-message/server";
import type { LayoutServerLoad } from "./$types";
import type { Fetch } from "$lib/fetch";

async function getLeauges(fetch: Fetch): Promise<PoeTradeLeagueResponse[]> {
  const poeApi = createPathOfExileApi(fetch);
  const leaguesResponse = await poeApi.getLeaguesList();
  return leaguesResponse.result;
}

export const load: LayoutServerLoad = loadFlash(async ({ fetch, url }) => {
  const leagues = await getLeauges(fetch);
  return { leagues, request_url: url.toString() };
})