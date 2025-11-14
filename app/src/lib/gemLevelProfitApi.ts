import { z } from 'zod';

export interface GemProfitRequestParameter {
	league: string;
	gem_name?: string | null;
	added_quality?: number | null;
	min_sell_price_chaos?: number | null;
	max_buy_price_chaos?: number | null;
	min_experience_delta?: number | null;
	min_listing_count?: number | null;
	items_count?: number | null;
}

export const gemProfitRequestParameterSchema = z.object({
	league: z.string(),
	gem_name: z.string().nullable().optional(),
	added_quality: z.number().min(0).max(100).nullable().optional(),
	min_sell_price_chaos: z.number().min(1).max(999).nullable().optional(),
	max_buy_price_chaos: z.number().min(1).max(999).nullable().optional(),
	min_experience_delta: z.number().min(200000000).max(2000000000).step(50000000).default(300000000),
	min_listing_count: z.number().min(1).max(100).step(10).default(20).nullable().optional(),
	items_count: z.number().nullable().optional().default(10)
});

export const gemProfitRequestParameterConstraints = {
	min_experience_delta: {
		defaultValue: gemProfitRequestParameterSchema.shape.min_experience_delta._def.defaultValue(),
		step: (
			gemProfitRequestParameterSchema.shape.min_experience_delta._def.innerType._def.checks.find(
				({ kind }) => kind === 'multipleOf'
			) as { value: number }
		).value,
		min: (
			gemProfitRequestParameterSchema.shape.min_experience_delta._def.innerType._def.checks.find(
				({ kind }) => kind === 'min'
			) as { value: number }
		).value,
		max: (
			gemProfitRequestParameterSchema.shape.min_experience_delta._def.innerType._def.checks.find(
				({ kind }) => kind === 'max'
			) as { value: number }
		).value
	},
	min_listing_count: {
		defaultValue:
			gemProfitRequestParameterSchema.shape.min_listing_count._def.innerType._def.innerType._def.defaultValue(),
		step: (
			gemProfitRequestParameterSchema.shape.min_listing_count._def.innerType._def.innerType._def.innerType._def.checks.find(
				({ kind }) => kind === 'multipleOf'
			) as { value: number }
		).value,
		min: (
			gemProfitRequestParameterSchema.shape.min_listing_count._def.innerType._def.innerType._def.innerType._def.checks.find(
				({ kind }) => kind === 'min'
			) as { value: number }
		).value,
		max: (
			gemProfitRequestParameterSchema.shape.min_listing_count._def.innerType._def.innerType._def.innerType._def.checks.find(
				({ kind }) => kind === 'max'
			) as { value: number }
		).value
	}
};

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
});

export type GemColor = 'white' | 'blue' | 'green' | 'red';
export const gemColorSchema = z.enum(['white', 'blue', 'green', 'red']);

export interface ProfitMargin {
	adjusted_earnings: number;
	experience_delta: number;
	gain_margin: number;
	quality_spent: number;
}

export const profitMarginSchema = z.object({
	adjusted_earnings: z.number(),
	experience_delta: z.number(),
	gain_margin: z.number(),
	quality_spent: z.number()
});

export interface ProfitResponseRecipes {
	quality_then_level: ProfitMargin;
	level_vendor_level: ProfitMargin;
}

export const profitResponseRecipesSchema = z.object({
	quality_then_level: profitMarginSchema,
	level_vendor_level: profitMarginSchema
});

export interface GemProfitResponseItem {
	name: string;
	icon?: string | null;
	color: GemColor;
	min: GemProfitResponseItemPrice;
	max: GemProfitResponseItemPrice;
	gain_margin: number;
	type: string;
	discriminator?: string | null;
	foreign_info_url: string;
	recipes: ProfitResponseRecipes;
}

export const gemProfitResponseItemSchema = z.object({
	name: z.string(),
	icon: z.string().nullable().optional(),
	color: gemColorSchema,
	min: gemProfitResponseItemPriceSchema,
	max: gemProfitResponseItemPriceSchema,
	gain_margin: z.number(),
	type: z.string(),
	discriminator: z.string().nullable().optional(),
	foreign_info_url: z.string(),
	recipes: profitResponseRecipesSchema
});

export type GemProfitResponse = GemProfitResponseItem[];

export const gemProfitResponseSchema = z.array(gemProfitResponseItemSchema);

export interface ExchangeRateToChaosRequestParameter {
	currency: CurrencyTypeName[];
	league: string;
}

export const exchangeRateToChaosRequestSchema = z.object({
	currency: z.array(z.string()),
	league: z.string()
});

export type ExchangeRateToChaosResponse = Record<CurrencyTypeName, number>;

export const exchangeRateToChaosResponseSchema = z.record(z.string(), z.number());

export type CurrencyTypeName = 'Divine Orb' | "Cartographer's Chisel" | 'Chaos Orb' | 'Vaal Orb';
