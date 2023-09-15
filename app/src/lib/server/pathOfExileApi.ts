import {
	type PoeTradeLeaguesResponse,
	type PoeTradeLeagueRequest,
	type PoeTradeLeagueResponse,
	type PoeTradeQueryRequest,
	type PoeTradeQueryResponse,
	type PoeTradeGemRequest,
	poeTradeLeaguesResponseSchema,
	poeTradeLeagueRequestSchema,
	poeTradeQueryRequestSchema,
	poeTradeQueryResponseDataSchema
} from '$lib/pathOfExileApi';

type Fetch = (input: RequestInfo | URL, init?: RequestInit | undefined) => Promise<Response>;

export interface PoeTradeLeagueApiOptions {
	api_endpoint: string;
}

export class PathofExileApi {
	fetch: Fetch;
	options: PoeTradeLeagueApiOptions;
	constructor(fetch: Fetch, options?: PoeTradeLeagueApiOptions) {
		this.options = options ?? { api_endpoint: 'https://www.pathofexile.com' };
		this.fetch = fetch;
	}

	getUserAgent() {
		return 'OAuth poe-gemleveling-profit-calculator/0.1 (contact: prophet.lamb@gmail.com)';
	}

	getLeaguesList: () => Promise<PoeTradeLeaguesResponse> = async () => {
		const headers = new Headers({
			Accept: 'application/json',
			'User-Agent': this.getUserAgent()
		});
		const url = new URL(`${this.options.api_endpoint}/api/trade/data/leagues`);
		const response = await this.fetch(url, {
			method: 'GET',
			headers
		});
		if (response.status !== 200) {
			throw new Error(`Request failed with status ${response.status}: ${await response.text()}`);
		}
		const rawResult = await response.json();
		const result = poeTradeLeaguesResponseSchema.parse(rawResult);
		return result;
	};

	getTradeLeague: (param: PoeTradeLeagueRequest) => Promise<PoeTradeLeagueResponse> = async (
		param
	) => {
		poeTradeLeagueRequestSchema.parse(param);
		const getLeaguesResponse = await this.getLeaguesList();
		const allRealmLeagues = getLeaguesResponse.result.filter(
			(league) => league.realm === param.realm
		);
		// the shortest text not 'Standard' is the softcore current trade league text
		const currentSoftcoreLeague = allRealmLeagues
			.filter(
				(league) =>
					!league.text.includes('Standard') &&
					!league.text.includes('Hardcore') &&
					!league.text.includes('Ruthless')
			)
			.reduce((a, b) => (a.text.length < b.text.length ? a : b));
		if (!currentSoftcoreLeague) {
			throw new Error(`No current league found for realm ${param.realm}`);
		}
		const league = getLeagueSwitch();
		if (!league) {
			throw new Error(
				`No league found for realm ${param.realm} and league mode ${param.trade_league}`
			);
		}
		return league;

		function getLeagueSwitch() {
			const hcRuthlessLeagueName = `HC Ruthless ${currentSoftcoreLeague.text}`;
			const ruthlessLeagueName = `Ruthless ${currentSoftcoreLeague.text}`;
			const hcLeagueName = `Hardcore ${currentSoftcoreLeague.text}`;
			switch (param.trade_league) {
				case 'Standard':
					return allRealmLeagues.find((league) => league.text === 'Standard');
				case 'Hardcore Ruthless':
					return allRealmLeagues.find((league) => league.text === hcRuthlessLeagueName);
				case 'Ruthless':
					return allRealmLeagues.find((league) => league.text === ruthlessLeagueName);
				case 'Hardcore':
					return allRealmLeagues.find((league) => league.text === hcLeagueName);
				case 'Softcore':
					return currentSoftcoreLeague;
				default:
					return undefined;
			}
		}
	};

	createTradeQuery: (param: PoeTradeQueryRequest) => Promise<PoeTradeQueryResponse> = async (
		param
	) => {
		poeTradeQueryRequestSchema.parse(param);
		const league = await this.getTradeLeague(param);
		const headers = new Headers({
			Accept: 'application/json',
			'Content-Type': 'application/json',
			'User-Agent': this.getUserAgent()
		});
		const url = new URL(`${this.options.api_endpoint}/api/trade/search/${league.id}`);
		const body = JSON.stringify(createTradeQueryBody());
		const response = await this.fetch(url, {
			method: 'POST',
			headers,
			body
		});
		if (response.status !== 200) {
			throw new Error(`Request failed with status ${response.status}: ${await response.text()}`);
		}
		const rawData = await response.json();
		const data = poeTradeQueryResponseDataSchema.parse(rawData);
		const result = {
			data,
			league,
			web_trade_url: `${this.options.api_endpoint}/trade/search/${league.id}?${data.id}`
		} satisfies PoeTradeQueryResponse;
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
				query: {
					filters: {
						misc_filters: {
							filters: {
								gem_level: {
									...(param.min_level !== undefined ? { min: param.min_level } : {}),
									...(param.max_level !== undefined ? { max: param.max_level } : {})
								},
								...(param.corrupted !== undefined
									? {
											corrupted: {
												option: param.corrupted ? 'true' : 'false'
											}
									  }
									: {})
							}
						},
						trade_filters: {
							filters: {
								collapse: {
									option: 'true'
								}
							}
						}
					},
					status: {
						option: 'online'
					},
					stats: [
						{
							type: 'and',
							filters: []
						}
					],
					type: param.name
				},
				sort: {
					price: 'asc'
				}
			};
		}
	};
}
