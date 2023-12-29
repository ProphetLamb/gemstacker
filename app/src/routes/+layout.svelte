<script lang="ts">
	import RootDrawer from './../lib/client/RootDrawer.svelte';
	import RootFooter from '$lib/client/RootFooter.svelte';
	import '../app.postcss';
	import { AppShell, Modal, type ModalComponent } from '@skeletonlabs/skeleton';
	import hljs from 'highlight.js';
	import 'highlight.js/styles/github-dark.css';
	import { storeHighlightJs } from '@skeletonlabs/skeleton';
	import { initializeStores } from '@skeletonlabs/skeleton';
	import { computePosition, autoUpdate, flip, shift, offset, arrow } from '@floating-ui/dom';
	import { storePopup } from '@skeletonlabs/skeleton';
	import type { LayoutServerLoad } from './$types';
	import GemFilterModal from '$lib/client/GemFilterModal.svelte';
	import RootHeader from '$lib/client/RootHeader.svelte';

	export let data: LayoutServerLoad;

	initializeStores();

	const modals: Record<string, ModalComponent> = {
		gemFilterModal: { ref: GemFilterModal }
	};
	storeHighlightJs.set(hljs);
	storePopup.set({ computePosition, autoUpdate, flip, shift, offset, arrow });
</script>

<Modal components={modals} />
<RootDrawer />

<div class="contents bg-image-blur h-full overflow-hidden">
	<AppShell>
		<svelte:fragment slot="header">
			<RootHeader {data} />
		</svelte:fragment>
		<svelte:fragment slot="footer">
			<RootFooter />
		</svelte:fragment>

		<!-- Page Route Content -->
		<slot />
	</AppShell>
</div>

<style lang="postcss">
	div.bg-image-blur {
		position: relative;

		&::before {
			content: '';
			position: absolute;
			top: 0;
			left: 0;
			width: 100%;
			height: 100%;
			background-size: contain;
			background-position: center;
			background-image: url(/background.png);
			background-repeat: no-repeat;
			z-index: -1;
		}

		& > div {
			backdrop-filter: blur(5px);
			backdrop-filter: url(#effect-blur-background);
			backdrop-filter: url("data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg'><filter id='effect-blur-background'><feGaussianBlur stdDeviation='5' /></filter></svg>#effect-blur-background");
		}
	}
</style>
