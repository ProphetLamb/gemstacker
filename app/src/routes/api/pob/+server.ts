import { downloadPob } from '$lib/pobWebsite';
import { error, json } from '@sveltejs/kit';

export const GET = async ({ fetch, url }) => {
	const src = decodeURIComponent(url.searchParams.get('src') ?? '');
	const headers = {
		'Cache-Control': 'public, max-age=31536000'
	};
	if (!src) {
		return json({ origin: 'raw', code: src }, { headers });
	}

	try {
		const pob = await downloadPob(src, fetch);
		return json({ origin: pob.web.label, code: pob.code}, { headers });
	} catch (err) {
		console.log('/api/pob:GET', err);
		error(500, 'could not download pob');
	}
};
