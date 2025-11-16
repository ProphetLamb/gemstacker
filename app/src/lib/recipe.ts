import type { GemProfitResponseItemRecipeName } from './gemLevelProfitApi';

type GemProfitRecipeInfo = {
	gem_cuttters?: number;
	vaal_orb?: number;
	description: string;
	title: string;
};

const literals = {
	buyLevel1: 'Purchase the level 1 gem.',
	buyVendor: 'Purchase the gem from a vendor',
	levelToMax: 'Gain experience until max level.',
	sell: 'Sell the gem.',
	vendor20Quality:
		"Vendor the max level Support Gem & 1 Gemcutter's Prism for level 1/20quality version.",
	qualityToMax: "Apply up to 20 Gemcutter's Prisms bringing the quality to maximum.",
	corruptAddLevel: 'Corrupt the gem for +1 Level',
	corruptForVaalSkill: "Corrupt the gem for it's Vaal version",
	noteCorruption60PercentChances:
		'60% success expectation is required for the corruption out come, instead of even odds'
};

function buildDescription(steps: string[], note?: string[]) {
	const recipe = steps
		.entries()
		.map(([i, x]) => `${i + 1}. ${x}`)
		.toArray()
		.join('\n');
	return !note ? recipe : `${recipe}\n${note.join('\n')}`;
}

const description = {
	level_corrupt_add_level_sell: buildDescription(
		[literals.buyLevel1, literals.levelToMax, literals.corruptAddLevel, literals.sell],
		[literals.noteCorruption60PercentChances]
	),
	level_sell: buildDescription([literals.buyLevel1, literals.levelToMax, literals.sell]),
	level_vendor_quality_level_sell: buildDescription([
		literals.buyLevel1,
		literals.levelToMax,
		literals.vendor20Quality,
		literals.levelToMax,
		literals.sell
	]),
	level_vendor_quality_sell: buildDescription([
		literals.buyLevel1,
		literals.levelToMax,
		literals.vendor20Quality,
		literals.sell
	]),
	quality_level_sell: buildDescription([
		literals.buyLevel1,
		literals.qualityToMax,
		literals.levelToMax,
		literals.sell
	]),
	vendor_buy_corrupt_level_sell_vaal: buildDescription(
		[literals.buyVendor, literals.corruptForVaalSkill, literals.levelToMax, literals.sell],
		[literals.noteCorruption60PercentChances]
	),
	vendor_buy_level_sell: buildDescription([literals.buyVendor, literals.levelToMax, literals.sell]),
	vendor_buy_level_vendor_quality_level_sell: buildDescription([
		literals.buyVendor,
		literals.levelToMax,
		literals.vendor20Quality,
		literals.levelToMax,
		literals.sell
	]),
	vendor_buy_level_vendor_quality_sell: buildDescription([
		literals.buyVendor,
		literals.levelToMax,
		literals.vendor20Quality,
		literals.sell
	]),
	vendor_buy_quality_level_sell: buildDescription([
		literals.buyVendor,
		literals.qualityToMax,
		literals.levelToMax,
		literals.sell
	])
} satisfies Record<GemProfitResponseItemRecipeName, string>;

const title = {
	level_corrupt_add_level_sell: 'Level > Corrupt +1 Level > Sell',
	level_sell: 'Level > Sell',
	level_vendor_quality_level_sell: 'Level > Vendor Quality > Level > Sell',
	level_vendor_quality_sell: 'Level > Vendor Quality > Sell',
	quality_level_sell: 'Quality > Level > Sell',
	vendor_buy_corrupt_level_sell_vaal: 'Vendor > Corrupt Vaal Skill > Level > Sell',
	vendor_buy_level_sell: 'Vendor > Level > Sell',
	vendor_buy_level_vendor_quality_level_sell: 'Vendor > Level > Vendor Quality > Level > Sell',
	vendor_buy_level_vendor_quality_sell: 'Vendor > Level > Vendor Quality > Sell',
	vendor_buy_quality_level_sell: 'Vendor > Quality > Level > Sell'
} satisfies Record<GemProfitResponseItemRecipeName, string>;

export function getRecipeInfo(recipe?: GemProfitResponseItemRecipeName): GemProfitRecipeInfo {
	if (!recipe) {
		return { description: '', title: '' };
	}
	const info = {
		description: description[recipe] ?? '',
		title: title[recipe] ?? ''
	} satisfies GemProfitRecipeInfo;
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
