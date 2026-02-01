import type { Fetch } from '$lib/fetch';
import {
	type PoeTradeLeaguesResponse,
	poeTradeLeaguesResponseSchema} from '$lib/pathOfExileApi';

export interface PoeTradeLeagueApiOptions {
	api_endpoint: string;
	web_endpoint: string;
}

export interface PathOfExileApi {
	getUserAgent(): string;
	getLeaguesList(): Promise<PoeTradeLeaguesResponse>;
}

export function createPathOfExileApi(fetch: Fetch, options?: PoeTradeLeagueApiOptions) {
	return new RawPathofExileApi(fetch, options);
}

function splitGemDiscriminator(name: string): [string, string | undefined] {
	const discriminators = ['Anomalous ', 'Divergent ', 'Phantasmal '];
	const discriminator = discriminators.find((discrimination) => name.startsWith(discrimination));
	if (!discriminator) {
		return [name, undefined];
	}
	const gemName = name.slice(discriminator.length);
	return [gemName, discriminator.trim()];
}

class RawPathofExileApi implements PathOfExileApi {
	fetch: Fetch;
	options: PoeTradeLeagueApiOptions;
	constructor(fetch: Fetch, options?: PoeTradeLeagueApiOptions) {
		this.options = options ?? {
			api_endpoint: 'https://www.pathofexile.com/api',
			web_endpoint: 'https://www.pathofexile.com'
		};
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
		const url = new URL(`${this.options.api_endpoint}/trade/data/leagues`);
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
}
