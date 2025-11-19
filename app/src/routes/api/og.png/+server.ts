import { API_ENDPOINT, API_KEY } from '$env/static/private';
import type { Fetch } from '$lib/fetch';
import {
	gemProfitRequestParameterConstraints,
	wellKnownExchangeRateDisplay,
	type GemProfitResponse,
	type WellKnownExchangeRateToChaosResponse
} from '$lib/gemLevelProfitApi';
import { gemProfitRequestParameterSchema } from '$lib/gemLevelProfitApi';
import { intlCompactNumber, intlFractionNumber, intlWholeNumber } from '$lib/intl';
import { createGemProfitApi } from '$lib/server/gemLevelProfitApi';
import { createPathOfExileApi } from '$lib/server/pathOfExileApi';
import { Resvg } from '@resvg/resvg-js';
import type { RequestHandler } from '@sveltejs/kit';
import satori from 'satori';
import { html as toReactElement } from 'satori-html';

const fontFile = await fetch('https://og-playground.vercel.app/inter-latin-ext-700-normal.woff');
const fontData: ArrayBuffer = await fontFile.arrayBuffer();

const height = 630;
const width = 1200;

const svgArrowRight = `
<svg tw="mx-4" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="3" stroke="currentColor" class="size-6">
	<path stroke-linecap="round" stroke-linejoin="round" d="M13.5 4.5 21 12m0 0-7.5 7.5M21 12H3" />
</svg>
`;

const template = (
	gemProfit: GemProfitResponse,
	exchangeRates: WellKnownExchangeRateToChaosResponse
) => {
	let html = `
	<div tw="bg-slate-800 flex flex-col w-full h-full items-center">
		<div tw="flex bg-slate-900">
			<div tw="flex flex-row w-full items-center justify-between px-4 py-2">
				<h2 tw="flex flex-row text-5xl font-bold tracking-tight text-gray-100 text-left">
					<span tw="mr-2">Gem Stacker:</span>
					<span tw="text-indigo-400">Gem leveling for profit</span>
				</h2>
			</div>
		</div>
		<div tw="flex w-full px-4">
			<ol tw="flex flex-col">
	`;
	for (const [idx, gem] of gemProfit.entries()) {
		const deltaExp = gem.max.experience - gem.min.experience;
		const deltaQty = Math.max(0, gem.max.quality - gem.min.quality);
		const deltaPrice = Math.max(0, gem.max.price - gem.min.price - deltaQty);
		const divineOrb = Math.floor(exchangeRates.divine_orb);
		const chaosOrb =
			Math.sign(deltaPrice) *
			(Math.abs(deltaPrice) - Math.floor(Math.abs(deltaPrice) / divineOrb) * divineOrb);
		html += `
				<li tw="flex text-gray-50 items-center text-3xl mt-4">
					<div tw="flex flex-row w-full justify-between items-center">
						<div tw="flex flex-row items-center">
						<div tw="flex w-16 h-16 bg-${gem.color === 'white' ? 'slate' : gem.color}-900 rounded-full mr-2">
						</div>
						<span>${gem.name}</span>
						</div>
						<div tw="flex flex-row items-center">
							<span tw="text-sky-300">${intlCompactNumber.format(deltaExp)}exp</span>
							${svgArrowRight}
							<span tw="text-lime-600">+</span>
						`;
		if (deltaPrice / divineOrb >= 0) {
			html += `
							<span tw="text-lime-600">${intlWholeNumber.format(deltaPrice / divineOrb)}</span>
							<img tw="h-8 w-8 mt-1" src="${wellKnownExchangeRateDisplay.divine_orb.img}" alt="${
				wellKnownExchangeRateDisplay.divine_orb.alt
			}" />
			`;
		}

		html += `
							<span tw="text-lime-600">${intlWholeNumber.format(chaosOrb)}</span>
							<img tw="h-8 w-8 mt-1" src="${wellKnownExchangeRateDisplay.chaos_orb.img}" alt="${
			wellKnownExchangeRateDisplay.chaos_orb.alt
		}" />
						</div>
					</div>
					<div tw="flex flex-row items-center text-xl">

					</div>
				</li>
		`;
	}
	html += `
			</ol>
		</div>
	</div>
	`;
	return html;
};

export const GET: RequestHandler = async ({ fetch }) => {
	const { gemProfit, exchangeRates } = (await defaultGemProfit(fetch)) ?? {
		exchangeRates: {},
		gemProfit: []
	};
	const html = template(gemProfit, exchangeRates as WellKnownExchangeRateToChaosResponse);
	return imageFromHtml(html);
};

async function imageFromHtml(html: string) {
	const dom = toReactElement(html);
	const svg = await satori(dom, {
		fonts: [
			{
				name: 'Inter Latin',
				data: fontData,
				style: 'normal'
			}
		],
		height,
		width
	});

	const resvg = new Resvg(svg, {
		fitTo: {
			mode: 'width',
			value: width
		}
	});

	const image = resvg.render();

	return new Response(image.asPng() as unknown as ReadableStream<Uint8Array<ArrayBuffer>>, {
		headers: {
			'Content-Type': 'image/png',
			'Cache-Control': 'public, immutable, no-transform, max-age=1800'
		}
	});
}

async function defaultGemProfit(
	fetch: Fetch
): Promise<
	{ gemProfit: GemProfitResponse; exchangeRates: WellKnownExchangeRateToChaosResponse } | undefined
> {
	try {
		const poeApi = createPathOfExileApi(fetch);
		const leaguesResponse = await poeApi.getLeaguesList();

		const request = gemProfitRequestParameterSchema.parse({
			league: leaguesResponse.result[0].text,
			min_experience_delta: gemProfitRequestParameterConstraints.min_experience_delta.defaultValue
		});
		const gemProfitApi = createGemProfitApi(fetch, {
			api_endpoint: API_ENDPOINT,
			api_key: API_KEY
		});
		const gemProfit = await gemProfitApi.getGemProfit(request);
		return {
			gemProfit: gemProfit.splice(0, 6),
			exchangeRates: await gemProfitApi.getWellKnownExchangeRatesToChaos(request.league)
		};
	} catch (err) {
		console.log('/api/og.png:getDefaultGemProfit', err);
		return undefined;
	}
}
