import { localStorageStore } from "@skeletonlabs/skeleton";

export interface LocalSettings {
  league?: string;
}

export const localSettings = localStorageStore<LocalSettings>('poe-gemleveling-profit-calculator-local-settings', {})