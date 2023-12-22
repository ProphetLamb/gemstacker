<script lang="ts">
	import '../app.postcss';
	import { AppShell, AppBar } from '@skeletonlabs/skeleton';
	import hljs from 'highlight.js';
	import 'highlight.js/styles/github-dark.css';
	import { storeHighlightJs } from '@skeletonlabs/skeleton';
	import { computePosition, autoUpdate, flip, shift, offset, arrow } from '@floating-ui/dom';
	import { storePopup } from '@skeletonlabs/skeleton';
	import { Icon } from '@steeze-ui/svelte-icon';
	import * as hi from '@steeze-ui/heroicons';
	storeHighlightJs.set(hljs);
	storePopup.set({ computePosition, autoUpdate, flip, shift, offset, arrow });
</script>

<div style="display: contents" class="bg-image-blur h-full overflow-hidden">
	<!-- App Shell -->
	<AppShell>
		<svelte:fragment slot="header">
			<!-- App Bar -->
			<AppBar>
				<svelte:fragment slot="lead">
					<a href="/" class="btn text-3xl uppercase font-extrabold">Gem Stacker</a>
					<a href="/loadout" class="btn hover:variant-soft-primary">Loadout</a>
					<a href="/single" class="btn hover:variant-soft-primary">Single Gem</a>
				</svelte:fragment>
				<svelte:fragment slot="trail">
					<a
						class="btn btn-sm variant-ghost-surface"
						href="https://github.com/ProphetLamb/poe-gemleveling-profit-calculator"
						target="_blank"
						rel="noreferrer"
					>
						<Icon src={hi.Star} size="16" theme="solid" class="fill-yellow-300" />
						<span>GitHub</span>
					</a>
				</svelte:fragment>
			</AppBar>
		</svelte:fragment>
		<svelte:fragment slot="pageFooter">
			<div class="flex flex-wrap items-center align-middle p-4 text-surface-600-300-token">
				<a href="/" target="_blank" rel="noreferrer" class="anchor">Home</a>
				&nbsp;
				<span class="opacity-10 mx-2">|</span>
				&nbsp;
				<a
					href="https://github.com/ProphetLamb/poe-gemleveling-profit-calculator/blob/master/LICENSE"
					target="_blank"
					rel="noreferrer"
					class="anchor">MIT License</a
				>
				&nbsp;
				<span class="opacity-10 mx-2">|</span>
				&nbsp;
				<a href="/imprint" target="_blank" rel="noreferrer" class="anchor">Imprint</a>
				&nbsp;
				<span class="opacity-10 mx-2">|</span>
				&nbsp;Powered by &nbsp;
				<a href="https://poe.ninja/" class="anchor flex flex-row items-center align-middle"
					><img src="https://poe.ninja/images/ninja-logo.png" alt="" class="w-4 h-4" /> poe.ninja</a
				>&nbsp;&amp;&nbsp;
				<a href="https://poedb.tw/us/" class="anchor flex flex-row items-center align-middle"
					><img src="https://poedb.tw/favicon.ico" alt="" class="w-4 h-4" /> poedb.tw</a
				>
			</div>
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
