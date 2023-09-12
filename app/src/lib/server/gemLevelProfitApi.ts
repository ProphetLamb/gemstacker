import { identity, pickBy } from 'lodash';

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

export class GemProfitApi {
	fetch: (input: RequestInfo | URL, init?: RequestInit | undefined) => Promise<Response>;
	options: GemProfitApiOptions;
	constructor(
		fetch: (input: RequestInfo | URL, init?: RequestInit | undefined) => Promise<Response>,
		options: GemProfitApiOptions
	) {
		this.options = options;
		this.fetch = fetch;
	}

	getGemProfit: (param: GemProfitRequestParameter) => Promise<GemProfitResponse> = async (
		param
	) => {
		const paramIdentiy = pickBy(
			{
				min_sell_price_chaos: param.min_sell_price_chaos?.toString(),
				max_buy_price_chaos: param.max_buy_price_chaos?.toString(),
				min_experience_delta: param.min_experience_delta?.toString(),
				items_offset: param.items_offset?.toString(),
				items_count: param.items_count?.toString()
			},
			identity
		) as Record<string, string>;

		const queryParam = new URLSearchParams(paramIdentiy);
		const headers = new Headers({
			Accept: 'application/json',
			Authorization: `Bearer ${btoa(this.options.api_key)}`
		});
		const url = new URL(`${this.options.api_endpoint}/gem-profit`);
		url.search = queryParam.toString();
		const response = await this.fetch(url.toString(), { headers });
		if (response.status !== 200) {
			throw new Error(`Request failed with status ${response.status}: ${await response.text()}`);
		}
		const result = (await response.json()) as GemProfitResponse;
		return result;
	};
}
