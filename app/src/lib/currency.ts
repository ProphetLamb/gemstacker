export type CurrencyDisplay =
	| 'ChaosThenDivieFraction'
	| 'DivineChaosRemainder'
	| 'ChaosFaction'
	| 'DivineFraction';

export const currencyDisplayValues = {
	ChaosThenDivieFraction: 'Chaos then Divine Orbs',
	DivineChaosRemainder: 'Divine- & Chaos Orbs',
	ChaosFaction: 'Chaos Orbs',
	DivineFraction: 'Divine Orbs'
} satisfies Record<CurrencyDisplay, string>;

export const defaultCurrencyDisplay = 'ChaosThenDivieFraction' as CurrencyDisplay;
