import { z } from 'zod';

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
