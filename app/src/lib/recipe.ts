import type { GemProfitResponseItemRecipeName } from './gemLevelProfitApi';

type GemProfitRecipeInfo = {
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
	corruptAddLevel: 'Corrupt the gem for +1 Level or +3 Quality',
	corruptAddLevelAddQuality: 'Double corrupt the gem for +1 Level and +3 Quality',
	corruptForVaalSkill: "Corrupt the gem for it's Vaal version"
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
	level_corrupt_add_level_sell: buildDescription([
		literals.buyLevel1,
		literals.levelToMax,
		literals.corruptAddLevel,
		literals.sell
	]),
	level_corrupt_add_level_and_quality_sell: buildDescription([
		literals.buyLevel1,
		literals.levelToMax,
		literals.corruptAddLevelAddQuality,
		literals.sell
	]),
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
	vendor_buy_corrupt_level_sell_vaal: buildDescription([
		literals.buyVendor,
		literals.corruptForVaalSkill,
		literals.levelToMax,
		literals.sell
	]),
	vendor_buy_level_corrupt_add_level_sell: buildDescription([
		literals.buyVendor,
		literals.levelToMax,
		literals.corruptAddLevel,
		literals.sell
	]),
	vendor_buy_level_corrupt_add_level_and_quality_sell: buildDescription([
		literals.buyVendor,
		literals.levelToMax,
		literals.corruptAddLevelAddQuality,
		literals.sell
	]),
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
	level_corrupt_add_level_sell: 'Level > Corrupt > Sell',
	level_corrupt_add_level_and_quality_sell: 'Level > Double Corrupt > Sell',
	level_sell: 'Level > Sell',
	level_vendor_quality_level_sell: 'Level > Vendor Quality > Level > Sell',
	level_vendor_quality_sell: 'Level > Vendor Quality > Sell',
	quality_level_sell: 'Quality > Level > Sell',
	vendor_buy_corrupt_level_sell_vaal: 'Vendor > Corrupt Vaal Skill > Level > Sell',
	vendor_buy_level_corrupt_add_level_sell: 'Vendor > Level > Corrupt > Sell',
	vendor_buy_level_corrupt_add_level_and_quality_sell: 'Vendor > Level > Double Corrupt > Sell',
	vendor_buy_level_sell: 'Vendor > Level > Sell',
	vendor_buy_level_vendor_quality_level_sell: 'Vendor > Level > Vendor Quality > Level > Sell',
	vendor_buy_level_vendor_quality_sell: 'Vendor > Level > Vendor Quality > Sell',
	vendor_buy_quality_level_sell: 'Vendor > Quality > Level > Sell'
} satisfies Record<GemProfitResponseItemRecipeName, string>;

export function getRecipeInfo(recipe?: GemProfitResponseItemRecipeName | string): GemProfitRecipeInfo {
	if (!recipe) {
		return { description: '', title: '' };
	}
	const key = recipe as GemProfitResponseItemRecipeName
	const info = {
		description: description[key] ?? '',
		title: title[key] ?? ''
	} satisfies GemProfitRecipeInfo;

	return info;
}
