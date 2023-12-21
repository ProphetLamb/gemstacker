import { z } from 'zod';

export interface GemProfitRequestParameter {
	gem_name?: string;
	min_sell_price_chaos?: number;
	max_buy_price_chaos?: number;
	min_experience_delta?: number;
	items_offset?: number;
	items_count?: number;
}

export const gemProfitRequestParameterSchema = z.object({
	league: z.string(),
	gem_name: z.string().optional(),
	min_sell_price_chaos: z.number().optional(),
	max_buy_price_chaos: z.number().optional(),
	min_experience_delta: z.number().min(1000000).max(100000000).default(1000000),
	items_offset: z.number().optional().default(0),
	items_count: z.number().optional().default(10)
});

export interface GemProfitResponseItemPrice {
	price: number;
	quality: number;
	level: number;
	experience: number;
	listing_count: number;
}

export const gemProfitResponseItemPriceSchema = z.object({
	price: z.number(),
	quality: z.number(),
	level: z.number(),
	experience: z.number(),
	listing_count: z.number()
})

export interface GemProfitResponseItem {
	name: string,
	icon?: string,
	min: GemProfitResponseItemPrice;
	max: GemProfitResponseItemPrice;
	gain_margin: number;
	discriminator?: string;
}

export const gemProfitResponseItemSchema = z.object({
	name: z.string(),
	icon: z.string().nullable(),
	min: gemProfitResponseItemPriceSchema,
	max: gemProfitResponseItemPriceSchema,
	gain_margin: z.number(),
	discriminator: z.string().nullable(),
});

export type GemProfitResponse = GemProfitResponseItem[]

export const gemProfitResponseSchema = z.array(gemProfitResponseItemSchema)