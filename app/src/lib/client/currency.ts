
export type CurrencyDisplay = 'DivineChaosRemainder' | 'ChaosFaction' | 'DivineFraction'

export const currencyDisplayValues = {'DivineChaosRemainder': 'Divine- & Chaos Orbs', 'ChaosFaction': 'Chaos Orbs', 'DivineFraction': 'Divine Orbs'} satisfies Record<CurrencyDisplay, string>

export const defaultCurrencyDisplay = 'DivineChaosRemainder' as CurrencyDisplay
