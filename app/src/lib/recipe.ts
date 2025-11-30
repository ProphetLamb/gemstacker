import type {
	GemProfitProbabilisticLabel,
	GemProfitResponseItemRecipeName
} from './gemLevelProfitApi';

type GemProfitRecipeInfo = {
	description: string;
	title: string;
	probabilistic?: boolean;
};

const literals = {
	buyLevel1: 'Purchase the Level 1 gem.',
	buyVendor: 'Purchase the gem from a vendor',
	levelToMax: 'Gain experience until the gem reaches max Level.',
	sell: 'Sell the gem.',
	vendor20Quality:
		"Vendor the max Level Support Gem & 1 Gemcutter's Prism for Level 1/20quality version.",
	qualityToMax: "Apply up to 20 Gemcutter's Prisms bringing the quality to maximum.",
	corruptAddLevel: 'Corrupt the gem for +1 Level or +3 Quality.',
	corruptAddLevelAddQuality: 'Double corrupt the gem for +1 Level and +3 Quality.',
	corruptForVaalSkill: "Corrupt the gem for it's Vaal version.",
	remLEvelDropFailure: 'If the Corruption resulted in -1 Level, sell the gem as is.',
	remLevelLevelFailure: 'If the Corruption resulted in -1 Level, level the gem back to max Level.'
};

function buildDescription(steps: string[], note?: string[]) {
	const recipe = steps
		.entries()
		.map(([i, x]) => `${i + 1}. ${x}`)
		.toArray()
		.join('\n');
	return !note ? recipe : `${recipe}\n${note.join('\n')}`;
}

export const wellKnownRecipeInfo = {
	level_corrupt_add_level_drop_failure_sell: {
		title: 'Level > Corrupt > Sell',
		description: buildDescription([
			literals.buyLevel1,
			literals.levelToMax,
			literals.corruptAddLevel,
			literals.remLEvelDropFailure,
			literals.sell
		]),
		probabilistic: true
	},
	level_corrupt_add_level_sell: {
		title: 'Level > Corrupt > Level -1 > Sell',
		description: buildDescription([
			literals.buyLevel1,
			literals.levelToMax,
			literals.corruptAddLevel,
			literals.remLevelLevelFailure,
			literals.sell
		]),
		probabilistic: true
	},
	level_corrupt_add_level_and_quality_sell: {
		title: 'Level > Double Corrupt > Sell',
		description: buildDescription([
			literals.buyLevel1,
			literals.levelToMax,
			literals.corruptAddLevelAddQuality,
			literals.sell
		]),
		probabilistic: true
	},
	level_sell: {
		title: 'Level > Sell',
		description: buildDescription([literals.buyLevel1, literals.levelToMax, literals.sell])
	},
	level_vendor_quality_level_sell: {
		title: 'Level > Vendor Quality > Level > Sell',
		description: buildDescription([
			literals.buyLevel1,
			literals.levelToMax,
			literals.vendor20Quality,
			literals.levelToMax,
			literals.sell
		])
	},
	level_vendor_quality_sell: {
		title: 'Level > Vendor Quality > Sell',
		description: buildDescription([
			literals.buyLevel1,
			literals.levelToMax,
			literals.vendor20Quality,
			literals.sell
		])
	},
	quality_level_sell: {
		title: 'Quality > Level > Sell',
		description: buildDescription([
			literals.buyLevel1,
			literals.qualityToMax,
			literals.levelToMax,
			literals.sell
		])
	},
	vendor_buy_corrupt_level_sell_vaal: {
		title: 'Vendor > Corrupt Vaal Skill > Level > Sell',
		description: buildDescription([
			literals.buyVendor,
			literals.corruptForVaalSkill,
			literals.levelToMax,
			literals.sell
		]),
		probabilistic: true
	},
	vendor_buy_level_corrupt_add_level_drop_failure_sell: {
		title: 'Vendor > Level > Corrupt > Sell',
		description: buildDescription([
			literals.buyLevel1,
			literals.levelToMax,
			literals.corruptAddLevel,
			literals.remLEvelDropFailure,
			literals.sell
		]),
		probabilistic: true
	},
	vendor_buy_level_corrupt_add_level_sell: {
		title: 'Vendor > Level > Corrupt > Level -1 > Sell',
		description: buildDescription([
			literals.buyVendor,
			literals.levelToMax,
			literals.corruptAddLevel,
			literals.remLevelLevelFailure,
			literals.sell
		]),
		probabilistic: true
	},
	vendor_buy_level_corrupt_add_level_and_quality_sell: {
		title: 'Vendor > Level > Double Corrupt > Sell',
		description: buildDescription([
			literals.buyVendor,
			literals.levelToMax,
			literals.corruptAddLevelAddQuality,
			literals.sell
		]),
		probabilistic: true
	},
	vendor_buy_level_sell: {
		title: 'Vendor > Level > Sell',
		description: buildDescription([literals.buyVendor, literals.levelToMax, literals.sell])
	},
	vendor_buy_level_vendor_quality_level_sell: {
		title: 'Vendor > Level > Vendor Quality > Level > Sell',
		description: buildDescription([
			literals.buyVendor,
			literals.levelToMax,
			literals.vendor20Quality,
			literals.levelToMax,
			literals.sell
		])
	},
	vendor_buy_level_vendor_quality_sell: {
		title: 'Vendor > Level > Vendor Quality > Sell',
		description: buildDescription([
			literals.buyVendor,
			literals.levelToMax,
			literals.vendor20Quality,
			literals.sell
		])
	},
	vendor_buy_quality_level_sell: {
		title: 'Vendor > Quality > Level > Sell',
		description: buildDescription([
			literals.buyVendor,
			literals.qualityToMax,
			literals.levelToMax,
			literals.sell
		])
	},
	misc: { title: 'Miscellaneous', description: '' }
} as Record<GemProfitResponseItemRecipeName | 'misc', GemProfitRecipeInfo>;

export function getRecipeInfo(
	recipe?: GemProfitResponseItemRecipeName | string
): GemProfitRecipeInfo {
	if (!recipe) {
		return wellKnownRecipeInfo.misc;
	}
	const key = recipe as GemProfitResponseItemRecipeName;
	return wellKnownRecipeInfo[key] ?? wellKnownRecipeInfo.misc;
}

export const wellKnownProbabilisticLabelDisplay = {
	corrupt_add_level: 'Corrupt +1 Level 20 Quality',
	corrupt_add_quality: 'Corrupt 23 Quality',
	corrupt_rem_quality: 'Corrupt 10 Quality',
	corrupt_rem_level: 'Corrupt -1 Level',
	double_corrupt_add_level_add_quality: 'Double corrupt +1 Level & 23 Quality',
	double_corrupt_add_level_rem_quality: 'Double corrupt +1 Level & 10 Quality',
	double_corrupt_add_level_max_quality: 'Double corrupt +1 Level & 20 Quality',
	double_corrupt_max_level_add_quality: 'Double corrupt max Level & 23 Quality',
	double_corrupt_corrupt_any_level_rem_quality: 'Double corrupt -1 or unchanged Level & 10 Quality',
	no_change: 'Unchanged',
	misc: 'Miscellaneous'
} satisfies Record<GemProfitProbabilisticLabel | 'misc', string>;

export function gainMarginToTextColor(gainMargin?: number) {
	if (typeof gainMargin !== 'number') {
		return '';
	}
	if (gainMargin > 0.1) {
		return 'text-secondary-700-200-token';
	}
	if (gainMargin > 0.05) {
		return 'text-warning-700-200-token';
	}
	return 'text-error-600-300-token';
}

export function gainMarginToBgColor(gainMargin?: number) {
	if (typeof gainMargin !== 'number') {
		return '';
	}
	if (gainMargin > 0.2) {
		return 'bg-lime-500/5';
	}
	if (gainMargin > 0.1) {
		return 'bg-sky-500/5';
	}
	if (gainMargin > 0.05) {
		return 'bg-amber-500/5';
	}
	return 'bg-red-500/10';
}

export function profitToTextColor(profit?: number) {
	if (typeof profit !== 'number') {
		return '';
	}
	if (profit > 0.1) {
		return 'text-success-200-700-token';
	}
	return 'text-error-300-600-token';
}
