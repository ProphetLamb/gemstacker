<script lang="ts">
	import { popup, type CssClasses, type PopupSettings } from '@skeletonlabs/skeleton';
	import { Icon } from '@steeze-ui/svelte-icon';
	import * as hi from '@steeze-ui/heroicons';
	import { createEventDispatcher } from 'svelte';
	import type { GemProfitResponseItem } from '$lib/gemLevelProfitApi';
	import { BetterTradingBookmarks, tradeBookmarkForGemBuy } from '$lib/betterTrading';
	import { copy } from '$lib/client/copy';

	export let data: GemProfitResponseItem[];
	export let textClass: CssClasses | null | undefined = undefined;
	let title = 'Gems';
	$: folderExport = createExport(data, title);

	function createExport(data: GemProfitResponseItem[], title: string) {
		const trades = data.map(tradeBookmarkForGemBuy);
		const bookmarks = new BetterTradingBookmarks();
		const betterTrading = bookmarks.serializeLegacy(
			{
				icon: null,
				title,
				archivedAt: null
			},
			trades
		);
		const tft = bookmarks.serialize(
			{
				icon: null,
				title,
				archivedAt: null
			},
			trades
		)
		return { betterTrading,  tft };
	}

	const betterTradingPopup: PopupSettings = {
		event: 'click',
		target: 'betterTradingPopup',
		placement: 'bottom',
		state: ({ state }) => {
			if (!state) {
				dispatch('close');
			} else {
				dispatch('open');
			}
		}
	};

	const dispatch = createEventDispatcher();

	let betterTradingInput: HTMLInputElement;
	let tftTradeExtensionInput: HTMLInputElement;
</script>

<button
	class="btn btn-sm variant-ghost-warning flex flex-row items-center py-0.5 {$$props.class}"
	title="Export Better Trading & TFT Trade Extension folder for the current gem results"
	use:popup={betterTradingPopup}
>
	<img class="w-5 h-5" src="/better-trading.png" alt="bt" />
	<img class="w-6 h-6" src="\tft.png" alt="tft" />
	<span class="max-md:hidden {textClass ?? ''}">Folder</span></button
>
<div class="z-10" data-popup="betterTradingPopup">
	<div class="w-72 md:w-96">
		<div class="arrow bg-surface-100-800-token" />
		<div
			class="card !opacity-100 flex flex-col items-stretch justify-start space-y-2 p-4 shadow-xl"
		>
			<div class="flex flex-row items-center justify-center w-full space-x-2 text-center">
				<h2 class="h2">Export Folder</h2>
			</div>
			<label class="label">
				<span>Title</span>
				<input name="title" class="input" type="text" bind:value={title} minlength="1" />
			</label>
			<hr class="!border-t-2 w-full opacity-50" />
			<label>
				<p class="flex flex-row items-center space-x-1">
					<img class="inline w-6 h-6" src="/tft.png" alt="tft" />
				<span>Export to TFT Trade Extension</span>
				</p>
				<div class="flex flex-row btn-group variant-ghost-primary">
					<button
						use:copy={{ value: () => tftTradeExtensionInput.value, on: 'click' }}
						class="btn variant-ghost-primary rounded-s-full rounded-e-none"
					>
						<Icon src={hi.Clipboard} size="16" />
					</button>
					<input
						bind:this={tftTradeExtensionInput}
						class="bg-transparent text-token border-none focus:outline-none outline-0 rounded-e-full w-full"
						type="text"
						disabled
						value={folderExport.tft}
					/>
				</div>
			</label>
			<label>
				<p class="flex flex-row items-center space-x-1">
					<img class="inline w-6 h-6" src="/better-trading.png" alt="bt" />
				 	<span>Export to Better Trading</span>
				</p>
				<div class="flex flex-row btn-group variant-ghost-primary">
					<button
						use:copy={{ value: () => betterTradingInput.value, on: 'click' }}
						class="btn variant-ghost-primary rounded-s-full rounded-e-none"
					>
						<Icon src={hi.Clipboard} size="16" />
					</button>
					<input
						bind:this={betterTradingInput}
						class="bg-transparent text-token border-none focus:outline-none outline-0 rounded-e-full w-full"
						type="text"
						disabled
						value={folderExport.betterTrading}
					/>
				</div>
			</label>
		</div>
	</div>
</div>
