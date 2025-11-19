<div class="flex flex-wrap justify-between min-w-fit w-full text-surface-600-300-token gap-2 {$$props.class ?? ''}">
	<p class="flex flex-wrap items-center whitespace-nowrap">
        <slot name="text" />
	</p>

	<div class="flex flex-row space-x-2">
		<slot name="buttons" />
	</div>
</div>
<hr class="w-full mt-2">
