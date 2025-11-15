import type { GemProfitResponseItem, WellKnownExchangeRateToChaosResponse } from "./gemLevelProfitApi";

export interface GemProfit {
  delta_exp: number,
  delta_price: number,
  delta_qty: number
}

export function getProfit(gem: GemProfitResponseItem, exchangeRates: WellKnownExchangeRateToChaosResponse | undefined): GemProfit {
  const delta_qty = Math.max(0, gem.max.quality - gem.min.quality)
  const cost_of_leveling = Math.max(0, delta_qty) * (exchangeRates?.gemcutters_prism ?? 1)
	return {
    delta_exp: gem.max.experience - gem.min.experience,
    delta_qty,
    delta_price: gem.max.price - gem.min.price - cost_of_leveling,
  }
}