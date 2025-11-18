export function isNullOrEmpty(obj: object | string | null | undefined) {
	if (obj === null || obj === undefined) {
		return true;
	}
	if (typeof obj === 'object') {
		for (const prop in obj) {
			if (Object.hasOwn(obj, prop)) {
				return false;
			}
		}
	} else if (typeof obj === 'string') {
		return (obj as string).trim().length === 0;
	}

	return true;
}

export function firstN<T>(arr: T[], items: number): T[] {
	return arr.slice(0, Math.min(arr.length, items));
}
