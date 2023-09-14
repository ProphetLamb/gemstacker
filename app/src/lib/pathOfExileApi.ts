import { z } from 'zod';

export const createGemTradeQuerySchema = z.object({
	name: z.string(),
	min_level: z.number().optional(),
	max_level: z.number().optional(),
	corrupted: z.boolean().optional()
});
