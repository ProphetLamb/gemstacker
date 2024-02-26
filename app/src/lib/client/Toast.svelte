<script lang="ts">
	import { toast } from '$lib/toast';
	import { ensureRootPath } from '$lib/url';
	import { Icon } from '@steeze-ui/svelte-icon';
	import * as hi from '@steeze-ui/heroicons';
</script>

<div class="max-w-md absolute z-50 top-2 left-0 right-0 ml-auto mr-auto space-y-4">
	{#each $toast as state}
		{@const toast = state.toast}
		{@const icon = !toast
			? undefined
			: toast.icon ||
			  (toast.type === 'success'
					? hi.CheckCircle
					: toast.type === 'warning'
					? hi.ExclamationCircle
					: toast.type === 'error'
					? hi.XCircle
					: hi.InformationCircle)}
		<div
			class="alert variant-filled-{toast.type} flex flex-row items-center space-x-2 space-y-0 shadow-lg opacity-90"
			on:click={() => state.dismiss()}
			role="presentation"
		>
			{#if icon}
				<Icon src={icon} class="icon w-8 h-8" />
			{/if}
			<p class="whitespace-break-spaces overflow-hidden overflow-ellipsis space-x-1">
				{#if typeof toast.message === 'string'}
					{toast.message}
				{:else}
					{#each toast.message as fragment}
						{#if typeof fragment === 'string'}
							<span class="">{fragment}</span>
						{:else}
							<a href={ensureRootPath(fragment.url)} class="anchor">{fragment.name}</a>
						{/if}
					{/each}
				{/if}
			</p>
		</div>
	{/each}
</div>
