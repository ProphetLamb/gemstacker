import type { GemProfitResponseItem, ProfitMargin } from "./gemLevelProfitApi";

export function getProfit(gem: GemProfitResponseItem): ProfitMargin {
  const recipies = Object.values(gem.recipes) as ProfitMargin[]
  return recipies.reduce((x,y) => x.adjusted_earnings > y.adjusted_earnings ? x : y)
}