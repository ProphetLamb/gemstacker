<script lang="ts">
	import { popup, type CssClasses, type PopupSettings } from '@skeletonlabs/skeleton';
	import { localSettings } from './localSettings';
	import { Icon } from '@steeze-ui/svelte-icon';
	import * as hi from '@steeze-ui/heroicons';
	import { createEventDispatcher } from 'svelte';
	import { deepEqual } from '$lib/compare';
	import { leagues } from './leagues';
	import LocalSettingsBasic from './LocalSettingsBasic.svelte';

	export let textClass: CssClasses = '';

	const initialLocalSettings = { ...$localSettings };
	const pcLeagues = $leagues.filter((l) => l.realm == 'pc');

	const dispatch = createEventDispatcher();

	const localSettingsPopup: PopupSettings = {
		event: 'click',
		target: 'localSettingsPopup',
		placement: 'bottom',
		state: ({ state }) => {
			if (!state) {
				dispatch('close', { changed: !deepEqual($localSettings, initialLocalSettings) });
			} else {
				dispatch('open');
			}
		}
	};
</script>

<button
	class="btn btn-sm variant-ghost-tertiary {$$props.class ?? ''}"
	use:popup={localSettingsPopup}
	><Icon src={hi.Cog6Tooth} size="16" theme="solid" /><span class="max-md:hidden {textClass ?? ''}"
		>Settings</span
	></button
>
<div class="">
	<div data-popup="localSettingsPopup" class="w-[calc(100%)] sm:w-[calc(100%-2rem)] pr-4 md:w-96">
		<div class="arrow bg-surface-100-800-token" />
		<div class="card flex flex-col items-stretch justify-start space-y-2 p-4 shadow-xl">
			<div class="flex flex-row justify-between items-center">
				<h2 class="h2">Settings</h2>
				<button class="btn-icon variant-ghost w-8 h-8"><Icon src={hi.XMark} size="16" /> </button>
			</div>
			<LocalSettingsBasic/>
			<hr class="!border-t-2 w-full opacity-50" />
			<a class="btn variant-soft-success" href="/settings"
				><Icon src={hi.Cog6Tooth} size="16" theme="solid" />
				<span>More Settings</span>
			</a>
		</div>
	</div>
</div>
