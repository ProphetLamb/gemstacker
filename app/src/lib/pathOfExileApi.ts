export type LeagueMode = 'Standard' | 'Softcore' | 'Hardcore' | 'Ruthless' | 'Hardcore Ruthless';
export type Realms = 'pc' | 'xbox' | 'sony';

export interface PoeTradeLeagueRequest {
	trade_league: LeagueMode
	realm: Realms
}

export interface PoeTradeLeagueResponse {
	id: string;
	realm: string;
	text: string;
}

export interface PoeTradeGemRequest {
	type: 'gem',
	name: string,
	min_level?: number,
	max_level?: number,
	corrupted?: boolean,
}

export interface PoeTradeRawRequest {
	type: 'raw',
	request: any,
}

export type PoeTradeQueryRequest = PoeTradeLeagueRequest & (PoeTradeGemRequest | PoeTradeRawRequest);

export interface PoeTradeQueryResponseData {
	/**
	 * The id of the query.
	 * This id is used to fetch the results of the query.
	 * Users can access the trade site at `https://www.pathofexile.com/trade/search/{league.id}?{id}`
	 */
	id: string;
	/**
	 * The ids of the specific results.
	 * These ids are used to fetch the specific results.
	 * Apis can fetch the specific results at `https://www.pathofexile.com/api/trade/fetch/{result.id}?query={id}`
	 */
	result: string[];
	/**
	 * The total number of results for the query.
	 */
	total: number;
}

export interface PoeTradeQueryResponse {
	data: PoeTradeQueryResponseData;
	league: PoeTradeLeagueResponse;
	web_trade_url: string;
}

export interface PoeTradeLeagueApiOptions {
}


interface PoeLeaguesResponse {
	result: PoeTradeLeagueResponse[]
}

type Fetch = (input: RequestInfo | URL, init?: RequestInit | undefined) => Promise<Response>;

export class PathofExileApi {
	fetch: Fetch;
	options: PoeTradeLeagueApiOptions;
	constructor(
		fetch: Fetch,
		options: PoeTradeLeagueApiOptions
	) {
		this.options = options;
		this.fetch = fetch;
	}

	getLeaguesList: () => Promise<PoeLeaguesResponse> = async () => {
		const headers = new Headers({
			Accept: 'application/json',
		});
		const url = new URL('https://www.pathofexile.com/api/trade/data/leagues');
		const response = await this.fetch(url, {
			method: 'GET',
			headers
		});
		if (response.status !== 200) {
			throw new Error(`Request failed with status ${response.status}: ${await response.text()}`);
		}
		const result = (await response.json()) as PoeLeaguesResponse;
		return result;
	}

	getTradeLeague: (param: PoeTradeLeagueRequest) => Promise<PoeTradeLeagueResponse> = async (
		param
	) => {
		const getLeaguesResponse = await this.getLeaguesList();
		const allRealmLeagues = getLeaguesResponse.result.filter(league => league.realm === param.realm);
		// the shortest text not 'Standard' is the softcore current trade league text
		const currentSoftcoreLeague = allRealmLeagues.filter(league => league.text !== 'Standard').reduce((a, b) => a.text.length < b.text.length ? a : b);
		if (!currentSoftcoreLeague) {
			throw new Error(`No current league found for realm ${param.realm}`);
		}
		const league = getLeagueSwitch();
		if (!league) {
			throw new Error(`No league found for realm ${param.realm} and league mode ${param.trade_league}`);
		}
		return league;

		function getLeagueSwitch() {
			const hcRuthlessLeagueName = `HC Ruthless ${currentSoftcoreLeague.text}`;
			const ruthlessLeagueName = `Ruthless ${currentSoftcoreLeague.text}`;
			const hcLeagueName = `Hardcore ${currentSoftcoreLeague.text}`;
			switch (param.trade_league) {
				case 'Standard':
					return allRealmLeagues.find(league => league.text === 'Standard');
				case 'Hardcore Ruthless':
					return allRealmLeagues.find(league => league.text === hcRuthlessLeagueName);
				case 'Ruthless':
					return allRealmLeagues.find(league => league.text === ruthlessLeagueName);
				case 'Hardcore':
					return allRealmLeagues.find(league => league.text === hcLeagueName);
				case 'Softcore':
					return currentSoftcoreLeague;
				default:
					return undefined;
			}
		}
	}

	createTradeQuery: (param: PoeTradeQueryRequest) => Promise<PoeTradeQueryResponse> = async (
		param
	) => {
		const league = await this.getTradeLeague(param);
		const headers = new Headers({
			Accept: 'application/json',
			'Content-Type': 'application/json'
		});
		const url = new URL(`https://www.pathofexile.com/api/trade/search/${league.id}`);
		const body = JSON.stringify(createTradeQueryBody())
		const response = await this.fetch(url, {
			method: 'POST',
			headers,
			body
		});
		if (response.status !== 200) {
			throw new Error(`Request failed with status ${response.status}: ${await response.text()}`);
		}
		const data = (await response.json()) as PoeTradeQueryResponseData;
		const result: PoeTradeQueryResponse = {
			data,
			league,
			web_trade_url: `https://www.pathofexile.com/trade/search/${league.id}?${data.id}`
		}
		return result;

		function createTradeQueryBody() {
			switch (param.type) {
				case 'gem':
					return createGemTradeQueryBody(param);
				case 'raw':
					return param.request;
				default:
					throw new Error(`Unknown trade query type`);
			}
		}

		function createGemTradeQueryBody(param: PoeTradeGemRequest) {
			return {
				"query": {
					"filters": {
						"misc_filters": {
							"filters": {
								"gem_level": {
									...(param.min_level !== undefined ?
										{ "min": param.min_level }
										: {}),
									...(param.max_level !== undefined ?
										{ "max": param.max_level }
										: {})
								},
								...(param.corrupted !== undefined ? {
									"corrupted": {
										"option": param.corrupted ? "true" : "false"
									}
								} : {})
							}
						},
						"trade_filters": {
							"filters": {
								"collapse": {
									"option": "true"
								}
							}
						}
					},
					"status": {
						"option": "online"
					},
					"stats": [
						{
							"type": "and",
							"filters": []
						}
					],
					"type": param.name
				},
				"sort": {
					"price": "asc"
				}
			}
		}
	}
}
