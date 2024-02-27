import { localStorageStore } from "@skeletonlabs/skeleton";
import { derived, type Invalidator, type Subscriber } from "svelte/store";
import { leagues } from "./leagues";

export interface LocalSettings {
  league: string
  min_experience_delta: number
  exclude_gems: string[]
}

function localSettingsStore() {
  const storage = localStorageStore<LocalSettings>('poe-gemleveling-profit-calculator-local-settings', {
    league: '',
    min_experience_delta: 340000000,
    exclude_gems: []
  })
  const reader = derived([storage, leagues], ([$storage, $leagues]) => {
    const league = $storage.league
    if ($leagues.length !== 0 && !$leagues.find(l => l.id == league)) {
      $storage.league = $leagues[0].id
    }
    return $storage
  })
  return {
    subscribe: reader.subscribe,
    set: storage.set,
    update: storage.update
  }
}


export const localSettings = localSettingsStore()