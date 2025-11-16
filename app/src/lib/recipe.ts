import type { GemProfitResponseItemRecipeName } from './gemLevelProfitApi';

type GemProfitRecipeInfo = {
	gem_cuttters?: number;
	description: string;
	title: string;
};

const literals = {
	buyLevel1: 'Purchase the level 1 gem.',
	levelToMax: 'Gain experience until max level.',
	sell: 'Sell the gem.',
	vendor20Quality: "Vendor the max level gem & 1 Gemcutter's Prism for level 1/20quality version.",
	qualityToMay: "Apply up to 20 Gemcutter's Prisms bringing the quality to maximum."
};

const description = {
	level_sell: `1. ${literals.buyLevel1}\n2. ${literals.levelToMax}\n3. ${literals.sell}`,
	level_vendor_quality_level_sell: `1. ${literals.buyLevel1}\n2. ${literals.levelToMax}\n3. ${literals.vendor20Quality}\n4. ${literals.levelToMax}\n5. ${literals.sell}`,
	level_vendor_quality_sell: `1. ${literals.buyLevel1}\n2. ${literals.levelToMax}\n3. ${literals.vendor20Quality}\n4. ${literals.sell}`,
	quality_level_sell: `1. ${literals.buyLevel1}\n2. ${literals.qualityToMay}\n3. ${literals.levelToMax}\n4. ${literals.sell}`
} satisfies Record<GemProfitResponseItemRecipeName, string>;

const title = {
	level_sell: 'Level > Sell',
	level_vendor_quality_level_sell: 'Level > Vendor Quality > Level > Sell',
	level_vendor_quality_sell: 'Level > Vendor Quality > Sell',
	quality_level_sell: 'Quality > Level > Sell'
} satisfies Record<GemProfitResponseItemRecipeName, string>;

export function getRecipeInfo(recipe: GemProfitResponseItemRecipeName): GemProfitRecipeInfo {
	const info = {
		description: description[recipe] ?? '',
		title: title[recipe] ?? ''
	} satisfies GemProfitRecipeInfo;
	if (!description[recipe]) {
		console.log('getRecipeInfo', 'Unknown recipe', recipe);
	}
	if (recipe === 'level_vendor_quality_level_sell') {
		return { ...info, gem_cuttters: 1 };
	}
	if (recipe === 'quality_level_sell') {
		return { ...info, gem_cuttters: 20 };
	}
	if (recipe === 'level_vendor_quality_sell') {
		return { ...info, gem_cuttters: 1 };
	}

	return info;
}
