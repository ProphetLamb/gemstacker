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

<!-- App Shell -->
<AppShell>
	<svelte:fragment slot="header">
		<!-- App Bar -->
		<AppBar>
			<svelte:fragment slot="lead">
				<a href="/" class="text-3xl uppercase font-extrabold">Gem Stacker</a>
			</svelte:fragment>
			<svelte:fragment slot="trail">
				<a
					class="btn btn-sm variant-ghost-surface"
					href="https://github.com/ProphetLamb/poe-gemleveling-profit-calculator"
					target="_blank"
					rel="noreferrer"
				>
					<Icon src={hi.Heart} size="16" theme="solid" class="fill-pink-600" />
					<span>GitHub</span>
				</a>
			</svelte:fragment>
		</AppBar>
	</svelte:fragment>
	<svelte:fragment slot="pageFooter">
		<div class="bg-surface-50-900-token p-4">
			<p>
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
			</p>
		</div>
	</svelte:fragment>
	<!-- Page Route Content -->
	<slot />
</AppShell>
