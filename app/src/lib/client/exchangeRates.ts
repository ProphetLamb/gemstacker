import type { WellKnownExchangeRateToChaosResponse } from "$lib/gemLevelProfitApi";
import { localStorageStore } from "@skeletonlabs/skeleton";

export const exchangeRates = localStorageStore<WellKnownExchangeRateToChaosResponse | undefined>('poe-gemleveling-profit-calculator-exchange-rates', undefined)