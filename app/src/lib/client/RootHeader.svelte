<script lang="ts">
	import { AppBar } from '@skeletonlabs/skeleton';
	import { Icon } from '@steeze-ui/svelte-icon';
	import * as hi from '@steeze-ui/heroicons';
	import LocalSettings from '$lib/client/LocalSettings.svelte';
	import type { PoeTradeLeagueResponse } from '$lib/pathOfExileApi';

	export let data: {
		leagues: PoeTradeLeagueResponse[];
	};
</script>

<AppBar>
	<svelte:fragment slot="lead">
		<a href="/" class="btn text-3xl uppercase font-extrabold"> Gem Stacker </a>
		<a href="/loadout" class="btn hover:variant-soft-primary">
			<Icon src={hi.Identification} size="24" />
			<span class="">Loadout</span>
		</a>
		<a href="/single" class="btn hover:variant-soft-primary">
			<Icon src={hi.AtSymbol} size="24" />
			<span class="">Single Gem</span>
		</a>
	</svelte:fragment>
	<svelte:fragment slot="trail">
		<a
			class="btn btn-sm variant-ghost-tertiary"
			href="https://github.com/ProphetLamb/poe-gemleveling-profit-calculator"
			target="_blank"
			rel="noreferrer"
		>
			<Icon src={hi.Star} size="16" theme="solid" class="fill-yellow-300" />
			<span class="hidden lg:inline-block">GitHub</span>
		</a>
		<LocalSettings {data} />
	</svelte:fragment>
</AppBar>
