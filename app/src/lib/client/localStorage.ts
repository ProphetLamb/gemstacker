import { localStorageStore } from '@skeletonlabs/skeleton';
import { isNullOrEmpty } from '../obj';
import type { Writable } from 'svelte/store';
import { browser } from '$app/environment';
interface Serializer<T> {
	parse(text: string): T;
	stringify(object: T): string;
}
type StorageType = 'local' | 'session';
interface Options<T> {
	serializer?: Serializer<T>;
	storage?: StorageType;
}
function getStorage(type: StorageType) {
	return type === 'local' ? localStorage : sessionStorage;
}

function ensureValidStorage<T>(key: string, options?: Options<T>) {
	const storageType = options?.storage ?? 'local';
	const serializer = options?.serializer ?? JSON;
	const storage = getStorage(storageType);
	const initialValue = storage.getItem(key);
	if (initialValue === 'undefined' || initialValue === 'null') {
		storage.removeItem(key);
	} else if (initialValue !== null) {
		try {
			serializer.parse(initialValue);
		} catch {
			storage.removeItem(key);
		}
	}
}

export function localStorageStoreOptional<T extends object>(key: string, options?: Options<T>) {
	if (browser) {
		ensureValidStorage(key, options);
	}
	const { set, subscribe, update } = localStorageStore<T | {}>(key, {});
	return {
		set: (v) => set(!isNullOrEmpty(v) ? (v as T) : {}),
		subscribe: (s, i) =>
			subscribe(
				(v) => s(!isNullOrEmpty(v) ? (v as T) : undefined),
				!!i ? (v) => i(!isNullOrEmpty(v) ? (v as T) : undefined) : undefined
			),
		update: (u) =>
			update((v) => {
				const r = u(!isNullOrEmpty(v) ? (v as T) : undefined);
				return !isNullOrEmpty(r) ? (r as T) : {};
			})
	} satisfies Writable<T | undefined>;
}
