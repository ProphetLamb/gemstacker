import { z } from "zod";
import type { GemProfitResponseItem } from "./gemLevelProfitApi";

export interface LoadoutRequest {
  league: string;
  red: number;
  green: number;
  blue: number;
  white: number;
  max_budget_chaos?: number | null;
}

export const loadoutRequestSchema = z.object({
  league: z.string(),
  red: z.number(),
  green: z.number(),
  blue: z.number(),
  white: z.number(),
  max_budget_chaos: z.number().optional().nullable()
})

export function computeBestLoadout(availableGems: GemProfitResponseItem[], request: LoadoutRequest): GemProfitResponseItem[] {
  return availableGems;
}