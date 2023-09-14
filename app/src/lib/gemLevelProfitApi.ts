import { z } from 'zod';

export const gemLevelsProfitSchema = z.object({
	gem_name: z.string().optional(),
	min_sell_price_chaos: z.number().optional(),
	max_buy_price_chaos: z.number().optional(),
	min_experience_delta: z.number().min(1000000).max(100000000).default(1000000),
	items_offset: z.number().optional().default(0),
	items_count: z.number().optional().default(10)
});

export interface GemProfitRequestParameter {
	gem_name?: string;
	min_sell_price_chaos?: number;
	max_buy_price_chaos?: number;
	min_experience_delta?: number;
	items_offset?: number;
	items_count?: number;
}

export const GemProfitRequestParameterSchema = z.object({
	gem_name: z.string().optional(),
	min_sell_price_chaos: z.number().optional(),
	max_buy_price_chaos: z.number().optional(),
	min_experience_delta: z.number().optional(),
	items_offset: z.number().optional(),
	items_count: z.number().optional()
});

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

export const GemProfitResponseSchema = z.object({
	data: z.record(
		z.object({
			min: z.object({
				price: z.number(),
				level: z.number(),
				exp: z.number()
			}),
			max: z.object({
				price: z.number(),
				level: z.number(),
				exp: z.number()
			}),
			gain_margin: z.number()
		})
	)
});

export interface GemProfitApiOptions {
	api_endpoint: string;
	api_key: string;
}
