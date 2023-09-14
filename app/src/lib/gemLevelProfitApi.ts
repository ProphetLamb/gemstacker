import { z } from 'zod';

export const gemProfitRequestParameterSchema = z.object({
	gem_name: z.string().optional(),
	min_sell_price_chaos: z.number().optional(),
	max_buy_price_chaos: z.number().optional(),
	min_experience_delta: z.number().min(1000000).max(100000000).default(1000000),
	items_offset: z.number().optional().default(0),
	items_count: z.number().optional().default(10)
});

export const gemProfitResponseSchema = z.object({
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
