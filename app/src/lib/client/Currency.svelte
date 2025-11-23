<script lang="ts">
	import { intlFractionNumber } from "$lib/intl";
	import type { CssClasses } from "@skeletonlabs/skeleton";
	import CurrencyIcon from "./CurrencyIcon.svelte";

    const defaultNumberFormat = intlFractionNumber

    export let value_prefix: number | string | undefined | null = undefined
    export let value
    export let value_class: CssClasses | undefined | null = undefined
    export let number_format: Intl.NumberFormat | undefined | null = defaultNumberFormat
    export let img
    export let alt
    export let img_class: CssClasses | undefined | null = undefined
</script>
<div class="flex flex-row items-center pr-1 lg:pr-0 {$$props.class ?? ''}">
    <span class={value_class ?? ''}>{value_prefix ?? ''}{(number_format ?? defaultNumberFormat).format(value)}</span>
    <CurrencyIcon {img} {alt} class={img_class} />
</div>