import {
	gemProfitRequestParameterSchema,
	gemProfitResponseSchema,
	type GemProfitRequestParameter,
	type GemProfitResponse
} from '$lib/gemLevelProfitApi';

export interface GemProfitApiOptions {
	api_endpoint: string;
	api_key: string;
}

type Fetch = (input: RequestInfo | URL, init?: RequestInit | undefined) => Promise<Response>;

export interface GemProfitApi {
	getGemProfit(param: GemProfitRequestParameter): Promise<GemProfitResponse>;
}

export function createGemProfitApi(fetch: Fetch, options: GemProfitApiOptions) {
	return new RawGemProfitApi(fetch, options);
}

export class RawGemProfitApi {
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
			.filter(([key, value]) => (key !== undefined && key !== null) && (value !== undefined && value !== null))
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
