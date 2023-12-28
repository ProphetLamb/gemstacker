export function replaceStateWithQuery<T extends Record<string, string | number | boolean>>(values: T, filter?: (values: T) => Partial<T>) {
  const url = new URL(window.location.toString());
  for (let [k, v] of Object.entries(!filter ? values : filter(values))) {
    if (!!v) {
      url.searchParams.set(encodeURIComponent(k), encodeURIComponent(v));
    } else {
      url.searchParams.delete(k);
    }
  }
  history.replaceState({}, '', url);
};

type NoUndefinedField<T> = { [P in keyof T]: Exclude<T[P], undefined> };
export function getStateFromQuery<T extends Record<string, string | number | boolean | undefined>>(filter: (value: Record<string, string>) => T): NoUndefinedField<T> {
  const url = new URL(window.location.toString());
  const query = Object.fromEntries(url.searchParams)
  const values = filter(query)
  const entires = Object.entries(values).filter(x => x !== undefined)
  // @ts-ignore: 2322
  return Object.fromEntries(entires)
}