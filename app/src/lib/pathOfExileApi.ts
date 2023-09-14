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
	corrupted?: boolean;
}

export const poeTradeGemRequestSchema = z.object({
	name: z.string(),
	min_level: z.number().optional(),
	max_level: z.number().optional(),
	corrupted: z.boolean().optional()
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
	result: string[];
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
	result: PoeTradeLeagueResponse[];
}

export const poeTradeLeaguesResponseSchema = z.object({
	result: z.array(poeTradeLeagueResponseSchema)
});
