export type LeagueMode = 'Standard' | 'Softcore' | 'Hardcore' | 'Ruthless' | 'Hardcore Ruthless';
export type Realms = 'pc' | 'xbox' | 'sony';

export interface PoeTradeLeagueRequest {
	trade_leauge: LeagueMode
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

export interface PoeTradeQueryResponse {
	id: string;
	result: string[];
	total: number;
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
			throw new Error(`No league found for realm ${param.realm} and league mode ${param.trade_leauge}`);
		}
		return league;

		function getLeagueSwitch() {
			const hcRuthlessLeagueName = `HC Ruthless ${currentSoftcoreLeague.text}`;
			const ruthlessLeagueName = `Ruthless ${currentSoftcoreLeague.text}`;
			const hcLeagueName = `Hardcore ${currentSoftcoreLeague.text}`;
			switch (param.trade_leauge) {
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
		const response = await this.fetch(url, {
			method: 'POST',
			headers,
			body: JSON.stringify(createTradeQueryBody())
		});
		if (response.status !== 200) {
			throw new Error(`Request failed with status ${response.status}: ${await response.text()}`);
		}
		const result = (await response.json()) as PoeTradeQueryResponse;
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
