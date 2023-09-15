import { Base64 } from 'js-base64';
import { z } from 'zod';

// based on https://github.com/exile-center/better-trading/

export interface BookmarksTradeLocation {
	type: string;
	slug: string;
}

export const bookmarksTradeLocationSchema = z.object({
	type: z.string(),
	slug: z.string()
});

export interface PartialBookmarksTradeLocation {
	type: string | null;
	slug: string | null;
}

export const partialBookmarksTradeLocationSchema = z.object({
	type: z.string().nullable(),
	slug: z.string().nullable()
});

export interface BookmarksTradeStruct {
	id?: string;
	title: string;
	location: BookmarksTradeLocation;
	completedAt: string | null;
}

export const bookmarksTradeStructSchema = z.object({
	id: z.string().optional(),
	title: z.string(),
	location: bookmarksTradeLocationSchema,
	completedAt: z.string().nullable()
});

export interface BookmarksFolderStruct {
	id?: string;
	title: string;
	icon: BookmarksFolderIcon | null;
	archivedAt: string | null;
}

export const bookmarksFolderStructSchema = z.object({
	id: z.string().optional(),
	title: z.string(),
	icon: z.string().nullable(),
	archivedAt: z.string().nullable()
});

export enum BookmarksFolderAscendancyDuelistIcon {
	SLAYER = 'slayer',
	GLADIATOR = 'gladiator',
	CHAMPION = 'champion'
}

export const bookmarksFolderAscendencyDuelistIconSchema = z.enum([
	BookmarksFolderAscendancyDuelistIcon.SLAYER,
	BookmarksFolderAscendancyDuelistIcon.GLADIATOR,
	BookmarksFolderAscendancyDuelistIcon.CHAMPION
]);

export enum BookmarksFolderAscendancyShadowIcon {
	ASSASSIN = 'assassin',
	SABOTEUR = 'saboteur',
	TRICKSTER = 'trickster'
}

export const bookmarksFolderAscendencyShadowIconSchema = z.enum([
	BookmarksFolderAscendancyShadowIcon.ASSASSIN,
	BookmarksFolderAscendancyShadowIcon.SABOTEUR,
	BookmarksFolderAscendancyShadowIcon.TRICKSTER
]);

export enum BookmarksFolderAscendancyMarauderIcon {
	JUGGERNAUT = 'juggernaut',
	BERSERKER = 'berserker',
	CHIEFTAIN = 'chieftain'
}

export const bookmarksFolderAscendencyMarauderIconSchema = z.enum([
	BookmarksFolderAscendancyMarauderIcon.JUGGERNAUT,
	BookmarksFolderAscendancyMarauderIcon.BERSERKER,
	BookmarksFolderAscendancyMarauderIcon.CHIEFTAIN
]);

export enum BookmarksFolderAscendancyWitchIcon {
	NECROMANCER = 'necromancer',
	ELEMENTALIST = 'elementalist',
	OCCULTIST = 'occultist'
}

export const bookmarksFolderAscendencyWitchIconSchema = z.enum([
	BookmarksFolderAscendancyWitchIcon.NECROMANCER,
	BookmarksFolderAscendancyWitchIcon.ELEMENTALIST,
	BookmarksFolderAscendancyWitchIcon.OCCULTIST
]);

export enum BookmarksFolderAscendancyRangerIcon {
	DEADEYE = 'deadeye',
	RAIDER = 'raider',
	PATHFINDER = 'pathfinder'
}

export const bookmarksFolderAscendencyRangerIconSchema = z.enum([
	BookmarksFolderAscendancyRangerIcon.DEADEYE,
	BookmarksFolderAscendancyRangerIcon.RAIDER,
	BookmarksFolderAscendancyRangerIcon.PATHFINDER
]);

export enum BookmarksFolderAscendancyTemplarIcon {
	GUARDIAN = 'guardian',
	HIEROPHANT = 'hierophant',
	INQUISITOR = 'inquisitor'
}

export const bookmarksFolderAscendencyTemplarIconSchema = z.enum([
	BookmarksFolderAscendancyTemplarIcon.GUARDIAN,
	BookmarksFolderAscendancyTemplarIcon.HIEROPHANT,
	BookmarksFolderAscendancyTemplarIcon.INQUISITOR
]);

export enum BookmarksFolderAscendancyScionIcon {
	ASCENDANT = 'ascendant'
}

export const bookmarksFolderAscendencyScionIconSchema = z.enum([
	BookmarksFolderAscendancyScionIcon.ASCENDANT
]);

export type BookmarksFolderAscendancyIcon =
	| BookmarksFolderAscendancyDuelistIcon
	| BookmarksFolderAscendancyShadowIcon
	| BookmarksFolderAscendancyMarauderIcon
	| BookmarksFolderAscendancyWitchIcon
	| BookmarksFolderAscendancyRangerIcon
	| BookmarksFolderAscendancyTemplarIcon
	| BookmarksFolderAscendancyScionIcon;

export const bookmarksFolderAscendencyIconSchema = z.union([
	bookmarksFolderAscendencyDuelistIconSchema,
	bookmarksFolderAscendencyShadowIconSchema,
	bookmarksFolderAscendencyMarauderIconSchema,
	bookmarksFolderAscendencyWitchIconSchema,
	bookmarksFolderAscendencyRangerIconSchema,
	bookmarksFolderAscendencyTemplarIconSchema,
	bookmarksFolderAscendencyScionIconSchema
]);

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

export const bookmarksFolderItemIconSchema = z.enum([
	BookmarksFolderItemIcon.ALCHEMY,
	BookmarksFolderItemIcon.CHAOS,
	BookmarksFolderItemIcon.EXALT,
	BookmarksFolderItemIcon.DIVINE,
	BookmarksFolderItemIcon.MIRROR,
	BookmarksFolderItemIcon.CARD,
	BookmarksFolderItemIcon.ESSENCE,
	BookmarksFolderItemIcon.FOSSIL,
	BookmarksFolderItemIcon.MAP,
	BookmarksFolderItemIcon.SCARAB
]);

export type BookmarksFolderIcon = BookmarksFolderAscendancyIcon | BookmarksFolderItemIcon;

export const bookmarksFolderIconSchema = z.union([
	bookmarksFolderAscendencyIconSchema,
	bookmarksFolderItemIconSchema
]);

interface ExportedFolderStruct {
	icn: string | null;
	tit: string;
	trs: Array<{
		tit: string;
		loc: string;
	}>;
}

const exportedFolderStructSchema = z.object({
	icn: z.string().nullable(),
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
			icn: folder.icon,
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

			const folder = {
				icon: potentialPayload.icn ? bookmarksFolderIconSchema.parse(potentialPayload.icn) : null,
				title: potentialPayload.tit,
				archivedAt: null
			} satisfies BookmarksFolderStruct;

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
