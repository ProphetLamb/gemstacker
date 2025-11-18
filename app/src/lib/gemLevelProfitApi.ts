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

export const gemProfitRequestParameterMinListingCountSchema = z
	.number()
	.min(0)
	.max(100)
	.step(10)
	.default(20)
	.nullable()
	.optional();

export const gemProfitRequestParameterSchema = z.object({
	league: z.string(),
	gem_name: z.string().nullable().optional(),
	added_quality: z.number().min(0).max(100).nullable().optional(),
	min_sell_price_chaos: z.number().min(1).max(999).nullable().optional(),
	max_buy_price_chaos: z.number().min(1).max(999).nullable().optional(),
	min_experience_delta: z.number().min(200000000).max(2000000000).step(50000000).default(300000000),
	min_listing_count: gemProfitRequestParameterMinListingCountSchema,
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
	corrupted: boolean;
	experience: number;
	listing_count: number;
}

export const gemProfitResponseItemPriceSchema = z.object({
	price: z.number(),
	quality: z.number(),
	level: z.number(),
	corrupted: z.boolean(),
	experience: z.number(),
	listing_count: z.number()
});

export type GemColor = 'white' | 'blue' | 'green' | 'red';
export const gemColorSchema = z.enum(['white', 'blue', 'green', 'red']);

export interface GemProfitProbabilisticProfitMargin {
	chance: number;
	earnings: number;
	label?: string | null;
}

export const gemProfitProbabilisticProfitMarginSchema = z.object({
	chance: z.number(),
	earnings: z.number(),
	label: z.string().optional().nullable()
});

export interface GemProfitResponeItemMargin {
	buy: GemProfitResponseItemPrice;
	sell: GemProfitResponseItemPrice;
	adjusted_earnings: number;
	experience_delta: number;
	gain_margin: number;
	recipe_cost?: Partial<Record<CurrencyTypeName, number>> | null;
	probabilistic?: GemProfitProbabilisticProfitMargin[] | null;
}

export const gemProfitResponseItemMarginSchema = z.object({
	buy: gemProfitResponseItemPriceSchema,
	sell: gemProfitResponseItemPriceSchema,
	adjusted_earnings: z.number(),
	experience_delta: z.number(),
	gain_margin: z.number(),
	recipe_cost: z.record(z.string(), z.number()).optional().nullable(),
	probabilistic: z.array(gemProfitProbabilisticProfitMarginSchema).optional().nullable()
});

export type GemProfitResponseItemRecipeName =
	| 'level_corrupt_add_level_sell'
	| 'level_corrupt_add_level_and_quality_sell'
	| 'level_sell'
	| 'level_vendor_quality_sell'
	| 'level_vendor_quality_level_sell'
	| 'quality_level_sell'
	| 'vendor_buy_corrupt_level_sell_vaal'
	| 'vendor_buy_level_corrupt_add_level_and_quality_sell'
	| 'vendor_buy_level_corrupt_add_level_sell'
	| 'vendor_buy_level_sell'
	| 'vendor_buy_level_vendor_quality_level_sell'
	| 'vendor_buy_level_vendor_quality_sell'
	| 'vendor_buy_quality_level_sell';

export type GemProfitResponseItemRecipes = Partial<
	Record<GemProfitResponseItemRecipeName, GemProfitResponeItemMargin>
>;

export const gemProfitResponseItemRecipesSchema = z.record(
	z.string(),
	gemProfitResponseItemMarginSchema
);

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
	preferred_recipe: GemProfitResponseItemRecipeName;
	recipes: GemProfitResponseItemRecipes;
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
	preferred_recipe: z.string(),
	recipes: gemProfitResponseItemRecipesSchema
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

export type CurrencyTypeName =
	| 'Divine Orb'
	| "Gemcutter's Prism"
	| "Cartographer's Chisel"
	| 'Chaos Orb'
	| 'Vaal Orb';

export interface WellKnownExchangeRateToChaosResponse {
	divine_orb: number;
	cartographers_chisel: number;
	gemcutters_prism: number;
	vaal_orb: number;
}

export interface CurrencyTypeDisplay {
	name: CurrencyTypeName;
	alt: string;
	img: string;
}

export const wellKnownExchangeRateDisplay = {
	divine_orb: { name: 'Divine Orb', alt: 'd', img: 'https://web.poecdn.com/image/Art/2DItems/Currency/CurrencyModValues.png' },
	cartographers_chisel: { name: "Cartographer's Chisel", alt: 'cc', img: 'https://web.poecdn.com/image/Art/2DItems/Currency/CurrencyMapQuality.png' },
	gemcutters_prism: { name: "Gemcutter's Prism", alt: 'gcp', img: 'https://web.poecdn.com/image/Art/2DItems/Currency/CurrencyGemQuality.png' },
	vaal_orb: { name: 'Vaal Orb', alt: 'v', img: 'https://web.poecdn.com/image/Art/2DItems/Currency/CurrencyVaal.png' },
	chaos_orb: { name: 'Chaos Orb', alt: 'c', img:  'https://web.poecdn.com/image/Art/2DItems/Currency/CurrencyRerollRare.png' }
} satisfies Record<keyof WellKnownExchangeRateToChaosResponse | 'chaos_orb', CurrencyTypeDisplay>;
