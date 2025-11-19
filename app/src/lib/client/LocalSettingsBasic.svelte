<script lang="ts">
	import { localSettings } from './localSettings';
	import { intlCompactNumber } from '$lib/intl';
	import { leagues } from './leagues';
	import { gemProfitRequestParameterConstraints } from '$lib/gemLevelProfitApi';
	import { currencyDisplayValues } from '../currency';

	const pcLeagues = $leagues.filter((l) => l.realm == 'pc');
</script>

<label class="label">
	<span>League</span>
	<select class="select" bind:value={$localSettings.league}>
		{#each pcLeagues as league}
			<option value={league.id}>{league.text}</option>
		{/each}
	</select>
</label>
<label class="label">
	<span>Minimum experience required for leveling</span>
	<input
		name="min_experience_delta"
		class="input"
		type="range"
		{...gemProfitRequestParameterConstraints.min_experience_delta}
		bind:value={$localSettings.min_experience_delta}
	/>
	<p>
		<span class="text-token">+{intlCompactNumber.format($localSettings.min_experience_delta)}</span
		><span class="text-sm text-surface-600-300-token">exp</span>
	</p>
</label>
<label class="label">
	<span>Minimum number of listings for gem</span>
	<input
		name="min_listing_count"
		class="input"
		type="range"
		bind:value={$localSettings.min_listing_count}
		{...gemProfitRequestParameterConstraints.min_listing_count}
	/>
	<p>
		<span class="text-token">{intlCompactNumber.format($localSettings.min_listing_count || 0)}</span
		><span class="text-sm text-surface-600-300-token">&nbsp;listing</span>
	</p>
</label>
<label class="label">
	<span>Display currency</span>
	<select name="currency_display" class="select" bind:value={$localSettings.currency_display}>
		{#each Object.entries(currencyDisplayValues) as [currencyDisplay, text]}
			<option value={currencyDisplay}>{text}</option>
		{/each}
	</select>
</label>
