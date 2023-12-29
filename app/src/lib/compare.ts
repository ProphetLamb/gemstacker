export function deepEqual(lhs: any, rhs: any): boolean {
  if (lhs === rhs) return true;

  if (Array.isArray(lhs) && Array.isArray(rhs)) {

    if (lhs.length !== rhs.length) return false;

    return lhs.every((elem, index) => {
      return deepEqual(elem, rhs[index]);
    })


  }

  if (typeof lhs === "object" && typeof rhs === "object" && lhs !== null && rhs !== null) {
    if (Array.isArray(lhs) || Array.isArray(rhs)) return false;

    const keys1 = Object.keys(lhs)
    const keys2 = Object.keys(rhs)

    if (keys1.length !== keys2.length || !keys1.every(key => keys2.includes(key))) return false;

    for (let key in lhs) {
      let isEqual = deepEqual(lhs[key], rhs[key])
      if (!isEqual) { return false; }
    }

    return true;
  }

  return false;
}