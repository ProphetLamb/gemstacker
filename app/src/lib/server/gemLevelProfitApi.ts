import { gemProfitRequestParameterSchema, gemProfitResponseSchema } from '$lib/gemLevelProfitApi';

export interface GemProfitRequestParameter {
	gem_name?: string;
	min_sell_price_chaos?: number;
	max_buy_price_chaos?: number;
	min_experience_delta?: number;
	items_offset?: number;
	items_count?: number;
}

export interface GemProfitResponse {
	data: {
		[key: string]: {
			min: {
				price: number;
				level: number;
				exp: number;
			};
			max: {
				price: number;
				level: number;
				exp: number;
			};
			gain_margin: number;
		};
	};
}

export interface GemProfitApiOptions {
	api_endpoint: string;
	api_key: string;
}

type Fetch = (input: RequestInfo | URL, init?: RequestInit | undefined) => Promise<Response>;

export class GemProfitApi {
	fetch: Fetch;
	options: GemProfitApiOptions;
	constructor(fetch: Fetch, options: GemProfitApiOptions) {
		this.options = options;
		this.fetch = fetch;
	}
	getGemProfit: (param: GemProfitRequestParameter) => Promise<GemProfitResponse> = async (
		param
	) => {
		gemProfitRequestParameterSchema.parse(param);

		const query = Object.entries(param)
			.map(([key, value]) => `${encodeURIComponent(key)}=${encodeURIComponent(value)}`)
			.join('&');
		const headers = new Headers({
			Accept: 'application/json',
			Authorization: `Bearer ${btoa(this.options.api_key)}`
		});
		const url = new URL(`${this.options.api_endpoint}/gem-profit?${query}`);
		const response = await this.fetch(url.toString(), { headers });
		if (response.status !== 200) {
			throw new Error(`Request failed with status ${response.status}: ${await response.text()}`);
		}
		const rawResult = await response.json();
		const result = gemProfitResponseSchema.parse(rawResult);
		return result;
	};
}
