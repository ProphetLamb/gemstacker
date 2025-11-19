export type Link = { name: string; url: string };

export function ensureRootPath(path: string | null | undefined) {
	if (!path) {
		return '/';
	}
	if (!path.startsWith('/')) {
		path = '/' + path;
	}
	return path;
}

/**
 * Creates a safe redirect to the URL
 * @param source The url inside this domain to redirect to
 * @returns the URI encoded path and search params relative to the domain root
 */
export function createRedirectTo(source: URL) {
	if (!source.searchParams.has('redirectTo')) {
		return encodeURIComponent(source.pathname + source.search);
	}
	return encodeURIComponent(ensureRootPath(source.searchParams.get('redirectTo')));
}

export function objectToQueryParams(param: object) {
	return Object.entries(param)
		.filter(
			([key, value]) => key !== undefined && key !== null && value !== undefined && value !== null
		)
		.flatMap(([key, value]) =>
			Array.isArray(value)
				? value.map((inner) => encodeKeyValue(key, inner))
				: encodeKeyValue(key, value)
		)
		.join('&');

	function encodeKeyValue(key: string, value: unknown) {
		return `${encodeURIComponent(key)}=${encodeURIComponent(value as string | number | boolean)}`;
	}
}

export function urlTrimSearch(url: URL | string): string {
	return url.toString().split(/[?#]/)[0].replace(/(\/)+$/, '')
}
