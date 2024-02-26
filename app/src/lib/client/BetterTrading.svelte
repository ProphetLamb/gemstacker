<script lang="ts">
	import { popup, type PopupSettings } from '@skeletonlabs/skeleton';
	import { Icon } from '@steeze-ui/svelte-icon';
	import * as hi from '@steeze-ui/heroicons';
	import { createEventDispatcher } from 'svelte';
	import type { GemProfitResponseItem } from '$lib/gemLevelProfitApi';
	import { BetterTradingBookmarks, tradeBookmarkForGemBuy } from '$lib/betterTrading';
	import { copy } from '$lib/client/copy';

	export let data: GemProfitResponseItem[];
	let title = 'Gems';
	$: betterTradingFolder = getBetterTradingFolder(data, title);

	function getBetterTradingFolder(data: GemProfitResponseItem[], title: string) {
		const trades = data.map(tradeBookmarkForGemBuy);
		const bookmarks = new BetterTradingBookmarks();
		const folder = bookmarks.serializeLegacy(
			{
				icon: null,
				title,
				archivedAt: null
			},
			trades
		);
		return folder;
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
</script>

<button class="btn btn-sm variant-ghost-warning flex" use:popup={betterTradingPopup}
	><img class="w-8 h-8" src="/better-trading.png" alt="" /> <span>Better Trading</span></button
>
<div class="">
	<div data-popup="betterTradingPopup" class="w-[calc(100%-2rem)] pr-4 md:w-96">
		<div class="arrow bg-surface-100-800-token" />
		<div class="card flex flex-col items-stretch justify-start space-y-2 p-4 shadow-xl">
			<div class="flex flex-row items-center justify-center w-full space-x-2 text-center">
				<img class="w-8 h-8 pt-1" src="/better-trading.png" alt="" />
				<h2 class="h2">Export Folder</h2>
			</div>
			<label class="label">
				<span>Folder Title</span>
				<input name="title" class="input" type="text" bind:value={title} minlength="1" />
			</label>
			<hr class="!border-t-2 w-full opacity-50" />
			<label for="label">
				<span>Folder Code</span>
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
						value={betterTradingFolder}
					/>
				</div>
			</label>
		</div>
	</div>
</div>
