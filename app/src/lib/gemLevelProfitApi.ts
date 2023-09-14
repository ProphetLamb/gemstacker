import { removeNull } from "$lib/typing";

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
	constructor(
		fetch: Fetch,
		options: GemProfitApiOptions
	) {
		this.options = options;
		this.fetch = fetch;
	}

	getGemProfit: (param: GemProfitRequestParameter) => Promise<GemProfitResponse> = async (
		param
	) => {
		const paramIdentity = removeNull({
			gem_name: param.gem_name?.toString(),
			min_sell_price_chaos: param.min_sell_price_chaos?.toString(),
			max_buy_price_chaos: param.max_buy_price_chaos?.toString(),
			min_experience_delta: param.min_experience_delta?.toString(),
			items_offset: param.items_offset?.toString(),
			items_count: param.items_count?.toString()
		});

		const queryParam = new URLSearchParams(paramIdentity);
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
