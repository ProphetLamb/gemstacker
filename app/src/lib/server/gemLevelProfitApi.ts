import {
	GemProfitRequestParameterSchema,
	type GemProfitApiOptions,
	type GemProfitRequestParameter,
	type GemProfitResponse,
	GemProfitResponseSchema
} from '$lib/gemLevelProfitApi';

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
		GemProfitRequestParameterSchema.parse(param);

		const queryParam = Object.entries(param)
			.map(([key, value]) => `${encodeURIComponent(key)}=${encodeURIComponent(value)}`)
			.join('&');
		const headers = new Headers({
			Accept: 'application/json',
			Authorization: `Bearer ${btoa(this.options.api_key)}`
		});
		const url = new URL(`${this.options.api_endpoint}/gem-profit?${queryParam}`);
		const response = await this.fetch(url.toString(), { headers });
		if (response.status !== 200) {
			throw new Error(`Request failed with status ${response.status}: ${await response.text()}`);
		}
		const rawResult = await response.json();
		const result = GemProfitResponseSchema.parse(rawResult);
		return result;
	};
}
