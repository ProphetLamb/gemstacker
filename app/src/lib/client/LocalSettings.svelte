<script lang="ts">
	import { popup, type CssClasses, type PopupSettings } from '@skeletonlabs/skeleton';
	import { localSettings, settingsAreDangerous } from './localSettings';
	import { Icon } from '@steeze-ui/svelte-icon';
	import * as hi from '@steeze-ui/heroicons';
	import { createEventDispatcher } from 'svelte';
	import { deepEqual } from '$lib/compare';
	import LocalSettingsBasic from './LocalSettingsBasic.svelte';

	export let textClass: CssClasses = '';

	const initialLocalSettings = { ...$localSettings };

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

	$: settingsTitle = settingsAreDangerous($localSettings)
		? 'One or more settings significantly affect reported profits and gems'
		: 'Open settings';
</script>

<button
	class="btn btn-sm variant-ghost-tertiary relative {$$props.class ?? ''}"
	use:popup={localSettingsPopup}
	title={settingsTitle}
	><Icon src={hi.Cog6Tooth} size="16" theme="solid" /><span class="max-md:hidden {textClass ?? ''}"
		>Settings</span
	>
	<div class="dangerous-settings {settingsAreDangerous($localSettings) ? '' : 'hidden'}" />
</button>
<aside data-popup="localSettingsPopup" class="w-[calc(100%)] sm:w-[calc(100%-2rem)] pr-4 md:w-96">
	<div class="arrow bg-surface-100-800-token" />
	<div class="card flex flex-col items-stretch justify-start space-y-2 p-4 shadow-xl">
		<div class="flex flex-row justify-between items-center">
			<h2 class="h2">Settings</h2>
			<button class="btn-icon variant-ghost w-8 h-8"><Icon src={hi.XMark} size="16" /> </button>
		</div>
		<LocalSettingsBasic />
		<hr class="!border-t-2 w-full opacity-50" />
		<a class="btn variant-soft-success relative" href="/settings" title={settingsTitle}
			><Icon src={hi.Cog6Tooth} size="16" theme="solid" />
			<span>More Settings</span>
			<div class="dangerous-settings {settingsAreDangerous($localSettings) ? '' : 'hidden'}" />
		</a>
	</div>
</aside>

<style lang="postcss">
	.dangerous-settings {
		@apply absolute bg-warning-500-400-token rounded-full top-1 left-0 block w-3 h-3 translate-x-[-50%] translate-y-[-50%];
	}
</style>
