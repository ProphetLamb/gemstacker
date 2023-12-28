import { localStorageStore } from "@skeletonlabs/skeleton";

export interface LocalSettings {
  league: string
  min_experience_delta: number
  exclude_gems: string[]
}

export const localSettings = localStorageStore<LocalSettings>('poe-gemleveling-profit-calculator-local-settings', {
  league: 'Standard',
  min_experience_delta: 340000000,
  exclude_gems: []
})