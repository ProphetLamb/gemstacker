import { sentrySvelteKit } from "@sentry/sveltekit";
import { purgeCss } from 'vite-plugin-tailwind-purgecss';
import { sveltekit } from '@sveltejs/kit/vite';
import { defineConfig } from 'vite';

export default defineConfig({
    plugins: [sentrySvelteKit({
        sourceMapsUploadOptions: {
            org: "prophetlamb-38d869310",
            project: "poe-gemleveling-profit-calculator",
            authToken: process.env.SENTRY_AUTH_TOKEN
        }
    }), sveltekit(), purgeCss()]
});