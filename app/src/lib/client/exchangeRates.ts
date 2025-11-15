import type { WellKnownExchangeRateToChaosResponse } from "$lib/gemLevelProfitApi";
import { localStorageStoreOptional } from "$lib/localStorage";

export const exchangeRates = localStorageStoreOptional<WellKnownExchangeRateToChaosResponse>('poe-gemleveling-profit-calculator-exchange-rates')