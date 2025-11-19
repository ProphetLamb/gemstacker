import type { Fetch } from './fetch';

export type PobWebsite = {
	label: string;
	id: string;
	matchURL: RegExp;
	regexURL: RegExp;
	downloadURL: string;
	codeOut?: string;
	postUrl?: string;
	postFields?: string;
	linkURL: string;
};

// taken from https://github.com/PathOfBuildingCommunity/PathOfBuilding/blob/dev/src/Modules/BuildSiteTools.lua
export const pobWebsiteList = [
	{
		label: 'Maxroll',
		id: 'Maxroll',
		matchURL: /maxroll\.gg\/poe\/pob\/.*/i,
		regexURL: /maxroll\.gg\/poe\/pob\/(.+)\s*$/i,
		downloadURL: 'maxroll.gg/poe/api/pob/$1',
		codeOut: 'https://maxroll.gg/poe/pob/',
		postUrl: 'https://maxroll.gg/poe/api/pob',
		postFields: 'pobCode:',
		linkURL: 'maxroll%.gg/poe/pob/$1'
	},
	{
		label: 'pobb.in',
		id: 'POBBin',
		matchURL: /pobb\.in\/.+/i,
		regexURL: /pobb\.in\/(.+)\s*$/i,
		downloadURL: 'pobb.in/pob/$1',
		codeOut: 'https://pobb.in/',
		postUrl: 'https://pobb.in/pob/',
		postFields: '',
		linkURL: 'pobb.in/$1'
	},
	{
		label: 'PoeNinja',
		id: 'PoeNinja',
		matchURL: /poe\.ninja\/?p?o?e?1?\/pob\/\w+/i,
		regexURL: /poe\.ninja\/?p?o?e?1?\/pob\/(\w+)\s*$/i,
		downloadURL: 'poe.ninja/poe1/pob/raw/$1',
		codeOut: '',
		postUrl: 'https://poe.ninja/poe1/pob/api/upload',
		postFields: 'code:',
		linkURL: 'poe.ninja/poe1/pob/$1'
	},
	{
		label: 'Pastebin.com',
		id: 'pastebin',
		matchURL: /pastebin\.com\/\w+/i,
		regexURL: /pastebin\.com\/(\w+)\s*$/i,
		downloadURL: 'pastebin.com/raw/$1',
		linkURL: 'pastebin.com/$1'
	},
	{
		label: 'PastebinP.com',
		id: 'pastebinProxy',
		matchURL: /pastebinp\.com\/\w+/i,
		regexURL: /pastebinp\.com\/(\w+)\s*$/i,
		downloadURL: 'pastebinp.com/raw/$1',
		linkURL: 'pastebin.com/$1'
	},
	{
		label: 'Rentry.co',
		id: 'rentry',
		matchURL: /rentry\.co\/\w+/i,
		regexURL: /rentry\.co\/(\w+)\s*$/i,
		downloadURL: 'rentry.co/paste/$1/raw',
		linkURL: 'rentry.co/$1'
	},
	{
		label: 'poedb.tw',
		id: 'PoEDB',
		matchURL: /poedb\.tw\/.+/i,
		regexURL: /poedb\.tw\/pob\/(.+)\s*$/i,
		downloadURL: 'poedb.tw/pob/$1/raw',
		codeOut: '',
		postUrl: 'https://poedb.tw/pob/api/gen',
		postFields: '',
		linkURL: 'poedb.tw/pob/$1'
	}
] satisfies PobWebsite[];

export async function downloadPob(url: string, fetch: Fetch): Promise<{ web: PobWebsite, code: string }> {
	const web = pobWebsiteList.find((x) => url.match(x.matchURL));
	if (!web) {
		throw new Error('Attempted download from unknown path of building website');
	}
	const src = url.replace(web.regexURL, web.downloadURL);
	const rsp = await fetch(src);
	const code = await rsp.text();
	return { web, code };
}
