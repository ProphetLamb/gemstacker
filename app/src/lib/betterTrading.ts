import type { GemProfitResponse } from '$lib/gemLevelProfitApi';
import { Base64 } from 'js-base64';
import { z } from 'zod';

// based on https://github.com/exile-center/better-trading/

export interface BookmarksTradeLocation {
	type: string;
	slug: string;
}

export interface PartialBookmarksTradeLocation {
	type: string | null;
	slug: string | null;
}

export interface BookmarksTradeStruct {
	id?: string;
	title: string;
	location: BookmarksTradeLocation;
	completedAt: string | null;
}

export interface BookmarksFolderStruct {
	id?: string;
	title: string;
	icon: BookmarksFolderIcon | null;
	archivedAt: string | null;
}

export type BookmarksFolderIcon = BookmarksFolderAscendancyIcon | BookmarksFolderItemIcon;

export enum BookmarksFolderAscendancyDuelistIcon {
	SLAYER = 'slayer',
	GLADIATOR = 'gladiator',
	CHAMPION = 'champion'
}

export enum BookmarksFolderAscendancyShadowIcon {
	ASSASSIN = 'assassin',
	SABOTEUR = 'saboteur',
	TRICKSTER = 'trickster'
}

export enum BookmarksFolderAscendancyMarauderIcon {
	JUGGERNAUT = 'juggernaut',
	BERSERKER = 'berserker',
	CHIEFTAIN = 'chieftain'
}

export enum BookmarksFolderAscendancyWitchIcon {
	NECROMANCER = 'necromancer',
	ELEMENTALIST = 'elementalist',
	OCCULTIST = 'occultist'
}

export enum BookmarksFolderAscendancyRangerIcon {
	DEADEYE = 'deadeye',
	RAIDER = 'raider',
	PATHFINDER = 'pathfinder'
}

export enum BookmarksFolderAscendancyTemplarIcon {
	GUARDIAN = 'guardian',
	HIEROPHANT = 'hierophant',
	INQUISITOR = 'inquisitor'
}

export enum BookmarksFolderAscendancyScionIcon {
	ASCENDANT = 'ascendant'
}

export type BookmarksFolderAscendancyIcon =
	| BookmarksFolderAscendancyDuelistIcon
	| BookmarksFolderAscendancyShadowIcon
	| BookmarksFolderAscendancyMarauderIcon
	| BookmarksFolderAscendancyWitchIcon
	| BookmarksFolderAscendancyRangerIcon
	| BookmarksFolderAscendancyTemplarIcon
	| BookmarksFolderAscendancyScionIcon;

export enum BookmarksFolderItemIcon {
	ALCHEMY = 'alchemy',
	CHAOS = 'chaos',
	EXALT = 'exalt',
	DIVINE = 'divine',
	MIRROR = 'mirror',
	CARD = 'card',
	ESSENCE = 'essence',
	FOSSIL = 'fossil',
	MAP = 'map',
	SCARAB = 'scarab'
}

interface ExportedFolderStruct {
	icn: string;
	tit: string;
	trs: Array<{
		tit: string;
		loc: string;
	}>;
}

const exportedFolderStructSchema = z.object({
	icn: z.string(),
	tit: z.string(),
	trs: z.array(
		z.object({
			tit: z.string(),
			loc: z.string()
		})
	)
});

function jsonFromExportString(exportString: string): string {
	if (exportString.startsWith('2:')) {
		// v2 export string, can include unicode emoji/etc
		return Base64.decode(exportString.slice(2));
	} else {
		// v1 export string with no version prefix, breaks for non-Latin1
		return atob(exportString);
	}
}

export class BetterTrading {
	serialize(folder: BookmarksFolderStruct, trades: BookmarksTradeStruct[]): string {
		const payload = {
			icn: folder.icon as string,
			tit: folder.title,
			trs: trades.map((trade) => ({
				tit: trade.title,
				loc: `${trade.location.type}:${trade.location.slug}`
			}))
		} satisfies ExportedFolderStruct;

		return `2:${Base64.encode(JSON.stringify(payload))}`;
	}

	deserialize(serializedFolder: string): [BookmarksFolderStruct, BookmarksTradeStruct[]] | null {
		try {
			const potentialPayload = exportedFolderStructSchema.parse(
				JSON.parse(jsonFromExportString(serializedFolder))
			);

			const folder: BookmarksFolderStruct = {
				icon: potentialPayload.icn as BookmarksFolderIcon,
				title: potentialPayload.tit,
				archivedAt: null
			};

			const trades: BookmarksTradeStruct[] = potentialPayload.trs.map((trade) => {
				const [type, slug] = trade.loc.split(':');

				return {
					title: trade.tit,
					completedAt: null,
					location: {
						type,
						slug
					}
				};
			});

			return [folder, trades];
		} catch (e) {
			return null;
		}
	}
}
