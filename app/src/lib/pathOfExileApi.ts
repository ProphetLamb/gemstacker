import { z } from 'zod';

export type LeagueMode = 'Standard' | 'Softcore' | 'Hardcore' | 'Ruthless' | 'Hardcore Ruthless';
export type Realms = 'pc' | 'xbox' | 'sony';

export interface PoeTradeLeagueRequest {
	trade_league: LeagueMode;
	realm: Realms;
}

export const poeTradeLeagueRequestSchema = z.object({
	trade_league: z.enum(['Standard', 'Softcore', 'Hardcore', 'Ruthless', 'Hardcore Ruthless']),
	realm: z.enum(['pc', 'xbox', 'sony'])
});

export interface PoeTradeLeagueResponse {
	id: string;
	realm: string;
	text: string;
}

export const poeTradeLeagueResponseSchema = z.object({
	id: z.string(),
	realm: z.string(),
	text: z.string()
});

export interface PoeTradeGemRequest {
	name: string;
	min_level?: number;
	max_level?: number;
	min_quality?: number;
	max_quality?: number;
	corrupted?: boolean;
	discriminator?: string | null;
}

export const poeTradeGemRequestSchema = z.object({
	name: z.string(),
	min_level: z.number().optional(),
	max_level: z.number().optional(),
	min_quality: z.number().optional(),
	max_quality: z.number().optional(),
	corrupted: z.boolean().optional(),
	discriminator: z.string().nullable().optional()
});

export interface PoeTradeRawRequest {
	request: unknown;
}

export const createRawTradeQuerySchema = z.object({
	request: z.unknown()
});

export type PoeTradeQueryRequest =
	| ({ type: 'gem' } & PoeTradeLeagueRequest & PoeTradeGemRequest)
	| ({ type: 'raw' } & PoeTradeLeagueRequest & PoeTradeRawRequest);

export const poeTradeQueryRequestSchema = z.union([
	z
		.object({ type: z.literal('gem') })
		.merge(poeTradeLeagueRequestSchema)
		.merge(poeTradeGemRequestSchema),
	z
		.object({ type: z.literal('raw') })
		.merge(poeTradeLeagueRequestSchema)
		.merge(createRawTradeQuerySchema)
]);

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
	result: Array<string>;
	/**
	 * The total number of results for the query.
	 */
	total: number;
	complexity?: number;
	inexact?: boolean;
}

export const poeTradeQueryResponseDataSchema = z.object({
	id: z.string(),
	result: z.array(z.string()),
	total: z.number(),
	complexity: z.number().optional(),
	inexact: z.boolean().optional()
});

export interface PoeTradeQueryResponse {
	data: PoeTradeQueryResponseData;
	league: PoeTradeLeagueResponse;
	web_trade_url: string;
}

export const poeTradeQueryResponseSchema = z.object({
	data: poeTradeQueryResponseDataSchema,
	league: poeTradeLeagueResponseSchema,
	web_trade_url: z.string().url()
});

export interface PoeTradeLeaguesResponse {
	result: Array<PoeTradeLeagueResponse>;
}

export const poeTradeLeaguesResponseSchema = z.object({
	result: z.array(poeTradeLeagueResponseSchema)
});

export function splitGemDiscriminator(name: string): [string, string | undefined] {
	const discriminators = ['Anomalous ', 'Divergent ', 'Phantasmal '];
	const discriminator = discriminators.find((discrimination) => name.startsWith(discrimination));
	if (!discriminator) {
		return [name, undefined];
	}
	const gemName = name.slice(discriminator.length);
	return [gemName, discriminator.trim()];
}

export function createGemTradeQueryBody(param: PoeTradeGemRequest) {
	let [name, discriminator] = splitGemDiscriminator(param.name);
	discriminator = param.discriminator ?? discriminator;
	return {
		query: {
			filters: {
				misc_filters: {
					filters: {
						gem_level: {
							...(param.min_level !== undefined ? { min: param.min_level } : {}),
							...(param.max_level !== undefined ? { max: param.max_level } : {})
						},
						quality: {
							...(param.min_quality !== undefined ? { min: param.min_quality } : {}),
							...(param.max_quality !== undefined ? { min: param.max_quality } : {})
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
				option: 'available'
			},
			stats: [
				{
					type: 'and',
					filters: []
				}
			],
			type: {
				...{ option: name },
				...(discriminator ? { discriminator: discriminator.toLowerCase() } : {})
			}
		},
		sort: {
			price: 'asc'
		}
	};
}
