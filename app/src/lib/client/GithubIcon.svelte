<script lang="ts">
	import { Icon } from '@steeze-ui/svelte-icon';
	import * as hi from '@steeze-ui/heroicons';
	import type { CssClasses } from '@skeletonlabs/skeleton';

	export let textClass: CssClasses = ''; // hidden lg:inline
</script>

<a
	class="btn btn-sm variant-ghost-tertiary {$$props.class ?? ''}"
	href="https://github.com/ProphetLamb/poe-gemleveling-profit-calculator"
	target="_blank"
	rel="noreferrer"
>
	<Icon src={hi.Star} size="16" theme="solid" class="fill-yellow-300" />
	<span class={textClass}>GitHub</span>
</a>
