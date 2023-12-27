import { boolean, z } from "zod";
import type { GemColor, GemProfitResponseItem } from "./gemLevelProfitApi";

export interface LoadoutRequest {
  league: string;
  red: number;
  green: number;
  blue: number;
  white: number;
  max_budget_chaos?: number | null;
}

const loadoutRequestSchemaNoSocketsError = "At least one socket required"

export const loadoutRequestSchema = z.object({
  league: z.string(),
  red: z.number().int().min(0).max(36).nullable().transform(v => v ?? 0),
  green: z.number().int().min(0).max(36).nullable().transform(v => v ?? 0),
  blue: z.number().int().min(0).max(36).nullable().transform(v => v ?? 0),
  white: z.number().int().min(0).max(36).nullable().transform(v => v ?? 0),
  max_budget_chaos: z.number().optional().nullable().transform(v => v ?? 0)
})
  .refine(req => maxCountLoadout(req) > 0, { message: loadoutRequestSchemaNoSocketsError, path: ["red"] })
  .refine(req => maxCountLoadout(req) > 0, { message: loadoutRequestSchemaNoSocketsError, path: ["green"] })
  .refine(req => maxCountLoadout(req) > 0, { message: loadoutRequestSchemaNoSocketsError, path: ["blue"] })
  .refine(req => maxCountLoadout(req) > 0, { message: loadoutRequestSchemaNoSocketsError, path: ["white"] })

export interface LoadoutResponseItem {
  gem: GemProfitResponseItem,
  count: number,
  socketColor: GemColor
}

function weight(item: GemProfitResponseItem): number {
  return item.min.price
}
function probability(item: GemProfitResponseItem): number {
  return item.gain_margin
}


function maxCountLoadout(request: LoadoutRequest, color?: GemColor) {
  if (color == "blue")
    return request.blue
  if (color == "green")
    return request.green
  if (color == "red")
    return request.red
  if (color == "white")
    return request.white
  return request.blue + request.green + request.red + request.white
}

type OptimizerItem = {
  gem: GemProfitResponseItem,
  socketColor: GemColor,
  idx: number
}

export class LoadoutOptimizer {
  constructor(request: LoadoutRequest, available: GemProfitResponseItem[]) {
    this.request = request
    this.available = available.sort(x => probability(x))
  }
  request: LoadoutRequest
  available: GemProfitResponseItem[]
  loadout: OptimizerItem[] = []

  countLoadout(color?: GemColor) {
    let count = 0;
    for (const item of this.loadout) {
      if (color && item.gem.color == color) {
        count += 1;
      }
    }
    return count
  }

  isLoadoutFull(color?: GemColor): boolean {
    return this.countLoadout(color) >= maxCountLoadout(this.request, color)
  }

  loadoutWeight(): number {
    let w = 0
    for (const item of this.loadout) {
      w += weight(item.gem)
    }
    return w
  }

  isLoadoutOverWeight(): boolean {
    return !!this.request.max_budget_chaos && this.loadoutWeight() >= this.request.max_budget_chaos
  }

  public optimize(): LoadoutResponseItem[] {
    this.initializeLoadout()
    this.optimizeLoadout()

    const response: LoadoutResponseItem[] = []
    for (const item of this.loadout) {
      let existing = response.find(x => x.socketColor === item.socketColor && x.gem.name === item.gem.name)
      if (!existing) {
        existing = { count: 0, gem: item.gem, socketColor: item.socketColor }
        response.push(existing)
      }

      existing.count += 1
    }
    return response
  }

  initializeLoadout() {
    this.loadout = []
    // fill white sockets with the best item
    this.fillLoadoutColorWithAvailable(0, "white");

    for (let idx = 0; idx < this.available.length && !this.isLoadoutFull(); idx += 1) {
      this.fillLoadoutColorWithAvailable(idx);
    }
  }

  fillLoadoutColorWithAvailable(idx: number, socketColor?: GemColor) {
    const item = this.available[idx]
    socketColor ??= item.color
    const maxCount = maxCountLoadout(this.request, socketColor);
    const curCount = this.countLoadout(socketColor);
    for (let c = curCount; c < maxCount; c += 1) {
      this.loadout.push({ gem: item, socketColor, idx });
    }
  }

  loadoutHeaviestIndex() {
    let worstIdx = -1
    let worstItem = undefined
    for (let idx = 0; idx < this.loadout.length; idx += 1) {
      const item = this.loadout[idx]
      if (!worstItem || weight(worstItem.gem) > weight(item.gem)) {
        worstIdx = idx
        worstItem = item
      }
    }
    return worstIdx
  }

  lightenLoadout(loadoutIdx: number): OptimizerItem | undefined {
    const loadoutItem = this.loadout[loadoutIdx]
    for (
      let idx = loadoutItem.idx;
      idx < this.available.length;
      idx++
    ) {
      const item = this.available[idx]
      if (weight(item) > weight(loadoutItem.gem)) {
        continue
      }
      if (item.color !== "white" && loadoutItem.socketColor !== "white" && item.color !== loadoutItem.socketColor) {
        continue
      }
      this.loadout[loadoutIdx] = { gem: item, socketColor: loadoutItem.socketColor, idx }
      break
    }
    const modifiedItem = this.loadout[loadoutIdx]
    if (loadoutItem === modifiedItem) {
      this.loadout.splice(loadoutIdx, 1)
      return undefined
    }
    return modifiedItem
  }

  optimizeLoadout() {
    while (this.isLoadoutOverWeight()) {
      const worstIdx = this.loadoutHeaviestIndex()
      if (worstIdx < 0) {
        break
      }

      this.lightenLoadout(worstIdx)
    }
  }
}
