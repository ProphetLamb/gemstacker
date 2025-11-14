import type { PoeTradeLeagueResponse } from "$lib/pathOfExileApi";
import { createPathOfExileApi } from "$lib/server/pathOfExileApi";
import { loadFlash } from "sveltekit-flash-message/server";
import type { LayoutServerLoad } from "./$types";

async function getLeauges(fetch: (input: RequestInfo | URL, init?: RequestInit | undefined) => Promise<Response>): Promise<PoeTradeLeagueResponse[]> {
  const poeApi = createPathOfExileApi(fetch);
  const leaguesResponse = await poeApi.getLeaguesList();
  return leaguesResponse.result;
}

export const load: LayoutServerLoad = loadFlash(async ({ fetch, url }) => {
  const leagues = await getLeauges(fetch);
  return { leagues, request_url: url.toString() };
})