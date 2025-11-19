<script lang="ts">
	import { intlFractionNumber } from "$lib/intl";
	import type { CssClasses } from "@skeletonlabs/skeleton";

    const defaultImgClass = 'h-4 w-4 mt-[0.1875rem] min-w-4'
    const defaultNumberFormat = intlFractionNumber

    export let value
    export let value_class: CssClasses | undefined | null = undefined
    export let src
    export let img_class: CssClasses | undefined | null = defaultImgClass
    export let alt
    export let number_format: Intl.NumberFormat | undefined | null = defaultNumberFormat
</script>
<div class="flex flex-row items-center pr-1 lg:pr-0 {$$props.class ?? ''}">
    <span class={value_class ?? ''}>{(number_format ?? defaultNumberFormat).format(value)}</span>
    <img {src} {alt} class={img_class ?? defaultImgClass} />
</div>