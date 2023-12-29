import * as hi from '@steeze-ui/heroicons';
export type List = Array<{ href: string, icon: hi.IconSource, title: string }>
export const menuNavLinks: List = [
  { href: "/loadout", icon: hi.Identification, title: "Loadout" },
  { href: "/single", icon: hi.AtSymbol, title: "Single Gem" },
]
