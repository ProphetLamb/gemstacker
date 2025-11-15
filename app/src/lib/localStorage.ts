import { localStorageStore } from "@skeletonlabs/skeleton"
import { isNullOrEmpty } from "./obj"
import type { Writable } from "svelte/store"

export function localStorageStoreAllowNull<T extends object>(key: string) {
  const { set, subscribe, update } = localStorageStore<T | {}>(key, {})
  return {
    set: (v) => set(!isNullOrEmpty(v) ? v as T : {}),
    subscribe: (s, i) => subscribe(
      v => s(!isNullOrEmpty(v) ? v as T : undefined),
      !!i ? (v => i(!isNullOrEmpty(v) ? v as T : undefined)) : undefined
    ),
    update: (u) => update((v) => {
      const r = u(!isNullOrEmpty(v) ? v as T : undefined)
      return !isNullOrEmpty(r) ? r as T : {}
    })
  } satisfies Writable<T | undefined>
}