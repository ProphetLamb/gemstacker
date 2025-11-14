<script lang="ts">
    export let request_url
	const request_path = request_url.split(/[?#]/)[0].replace(/\/$/, '')

    const description = "Computes the profit gained per experience invested in a gem. Useful to make a net profit on 5-way rotas, and similar mechanics."
    const title = "Gem Stacker - Gem leveling for profit"
</script>

<meta property="title" content="{title}" />
<meta property="description" content="{description}" />
<meta property="twitter:image" content="{request_path}/api/og.png" />
<meta property="twitter:site" content="@github" />
<meta property="twitter:card" content="summary_large_image" />
<meta property="twitter:title" content="{title}" />
<meta property="twitter:description" content="{description}" />
<meta property="og:type" content="object" />
<meta property="og:locale" content="en-US" />
<meta property="og:locale:alternate" content="en-GB" />
<meta property="og:site_name" content="GemStacker" />
<meta property="og:image" content="{request_path}/api/og.png" />
<meta property="og:image:alt" content="List of gems, cost of acquistion, proft from sale" />
<meta property="og:image:width" content="1200" />
<meta property="og:image:height" content="630" />
<meta property="og:title" content="" />
<meta property="og:url" content="{request_url}" />
<meta property="og:description" content="{description}" />
<meta property="hostname" content="{new URL(request_path).hostname}" />
<meta property="expected-hostname" content="{new URL(request_path).hostname}" />