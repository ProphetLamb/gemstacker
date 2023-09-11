import type { Actions } from '@sveltejs/kit';
import type { PageServerLoad } from './$types';

const load: PageServerLoad = async ({ params, locals }) => {};

export const actions: Actions = {
	default: async () => {}
};
