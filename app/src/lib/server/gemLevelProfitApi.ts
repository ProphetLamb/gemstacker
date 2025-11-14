import type { Fetch } from '$lib/fetch';
import {
	exchangeRateToChaosRequestSchema as exchangeRateToChaosRequestParameterSchema,
	exchangeRateToChaosResponseSchema,
	gemProfitRequestParameterSchema,
	gemProfitResponseSchema,
	type ExchangeRateToChaosRequestParameter,
	type ExchangeRateToChaosResponse,
	type GemProfitRequestParameter,
	type GemProfitResponse
} from '$lib/gemLevelProfitApi';

export interface GemProfitApiOptions {
	api_endpoint: string;
	api_key: string;
}

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

	defaultHeaders = () =>
		new Headers({
			Accept: 'application/json',
			Authorization: `Bearer ${btoa(this.options.api_key)}`
		});

	getGemProfit: (param: GemProfitRequestParameter) => Promise<GemProfitResponse> = async (
		param
	) => {
		gemProfitRequestParameterSchema.parse(param);

		const query = objectToQueryParams(param);
		const headers = this.defaultHeaders();
		const url = new URL(`${this.options.api_endpoint}/gem-profit?${query}`);
		const response = await this.fetch(url.toString(), { headers });
		if (response.status !== 200) {
			throw new Error(`Request failed with status ${response.status}: ${await response.text()}`);
		}
		const rawResult = await response.json();
		const result = gemProfitResponseSchema.parse(rawResult);
		return result;
	};

	exchangeRateToChaos: (
		param: ExchangeRateToChaosRequestParameter
	) => Promise<ExchangeRateToChaosResponse> = async (param) => {
		exchangeRateToChaosRequestParameterSchema.parse(param);

		const query = objectToQueryParams(param);
		const headers = this.defaultHeaders();
		const url = new URL(`${this.options.api_endpoint}/exchange-rate-to-chaos?${query}`);
		const response = await this.fetch(url.toString(), { headers });
		if (response.status !== 200) {
			throw new Error(`Request failed with status ${response.status}: ${await response.text()}`);
		}
		const rawResult = await response.json();
		const result = exchangeRateToChaosResponseSchema.parse(rawResult) as ExchangeRateToChaosResponse;
		return result;
	};
}

function objectToQueryParams(param: object) {
	return Object.entries(param)
		.filter(
			([key, value]) => key !== undefined && key !== null && value !== undefined && value !== null
		)
		.flatMap(([key, value]) =>
			Array.isArray(value)
				? value.map((inner) => encodeKeyValue(key, inner))
				: encodeKeyValue(key, value)
		)
		.join('&');

	function encodeKeyValue(key: string, value: unknown) {
		return `${encodeURIComponent(key)}=${encodeURIComponent(value as string | number | boolean)}`;
	}
}
