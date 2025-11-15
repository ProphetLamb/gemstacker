import type { WellKnownExchangeRateToChaosResponse } from "$lib/gemLevelProfitApi";
import { localStorageStoreAllowNull } from "$lib/localStorage";

export const exchangeRates = localStorageStoreAllowNull<WellKnownExchangeRateToChaosResponse>('poe-gemleveling-profit-calculator-exchange-rates')