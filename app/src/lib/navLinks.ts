import type { CssClasses } from '@skeletonlabs/skeleton';
import * as hi from '@steeze-ui/heroicons';

interface NavItemStyle {
	icon?: hi.IconSource;
	title: string;
	class?: CssClasses;
}

export type NavLink = NavItemStyle & {
	href: string;
	decoration?: NavItemStyle;
}
export const menuNavLinks = [
	{
		href: '/single',
		icon: hi.MagnifyingGlass,
		title: 'Search',
		class: 'relative',
		decoration: {
			title: 'Advanced',
			class:
				'btn variant-filled-warning absolute top-8 left-12 py-0.5 px-1 text-center justify-center flex flex-row text-xs hover:brightness-100'
		}
	},
	{ href: '/loadout', icon: hi.Identification, title: 'Loadout' },
	{
		href: '/faq',
		icon: hi.BookOpen,
		title: 'FAQ'
	}
] as NavLink[];
