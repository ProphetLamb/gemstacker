<script lang="ts">
	import { getModalStore, type ModalSettings } from '@skeletonlabs/skeleton';
	import GemFilterModal from './GemFilterModal.svelte';
	import * as hi from '@steeze-ui/heroicons';
	import { Icon } from '@steeze-ui/svelte-icon';

	const modalStore = getModalStore();
	function gemFilterModal() {
		const modal: ModalSettings = {
			type: 'component',
			component: { ref: GemFilterModal },
			title: 'Exclude unwanted gems',
			body: ''
		};
		modalStore.trigger(modal);
	}
</script>

<button
	type="button"
	class="btn btn-sm text-token variant-ghost-tertiary shadow"
	on:click={gemFilterModal}
>
	<Icon src={hi.Funnel} size="16" />
	<span class="hidden lg:inline mr-0 5">Filter</span>
</button>
