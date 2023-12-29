export function replaceStateWithQuery<T extends Record<string, string | number | boolean | undefined>>(values: T, filter?: (values: T) => Partial<T>) {
  const url = locationWithSearch(!filter ? values : filter(values), window.location.toString())
  history.replaceState({}, '', url);
};

type NoUndefinedField<T> = { [P in keyof T]: Exclude<T[P], undefined> };
export function getStateFromQuery<T extends Record<string, string | number | boolean | undefined>>(filter: (value: Record<string, string>) => T): NoUndefinedField<T> {
  const url = new URL(window.location.toString());
  const query = Object.fromEntries(url.searchParams)
  const values = filter(query)
  const entires = Object.entries(values).filter(x => x[1] !== undefined && x[1] !== null)
  // @ts-ignore: 2322
  return Object.fromEntries(entires)
}

export function locationWithSearch<T extends Record<string, string | number | boolean | undefined>>(searchParams: T, location?: URL | string): URL {
  const exampleLocation = "http://example.com"
  const url = location === undefined ? new URL(exampleLocation) : new URL(location, exampleLocation)
  const entires = Object.entries(searchParams)
  for (let [k, v] of entires) {
    if (v !== undefined && v !== null) {
      url.searchParams.set(encodeURIComponent(k), encodeURIComponent(v));
    } else {
      url.searchParams.delete(k);
      url.searchParams.delete(encodeURIComponent(k));
    }
  }
  return url
}

declare global {
  interface URL {
    pathSearchHash(): string
  }
}

URL.prototype.pathSearchHash = function (): string {
  return `${this.pathname}${this.search}${this.hash}`
}