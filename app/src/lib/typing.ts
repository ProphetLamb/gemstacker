export function removeNull<K extends string | number | symbol, T>(obj: Record<K, T | null | undefined>): Record<K, T> {
	const result: Record<K, T> = {} as Record<K, T>;
	for (const key in obj) {
		if (obj[key] !== null && obj[key] !== undefined) {
			result[key] = obj[key] as T;
		}
	}
	return result;
}
