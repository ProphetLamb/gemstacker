<script>
	import Currency from '$lib/client/Currency.svelte';
	import {
		gemProfitResponseItemRecipeName,
		wellKnownExchangeRateDisplay
	} from '$lib/gemLevelProfitApi';
	import { getRecipeInfo, wellKnownProbabilisticLabelDisplay } from '$lib/recipe';
</script>

<div class="w-full h-full">
	<div
		class="article m-auto lg:max-w-screen-lg flex flex-col items-center justify-center space-y-4"
	>
		<h1 class="h1 p-4">FAQ</h1>
		<div class="card p-4 w-full text-start shadow-xl">
			<a href="#basic_workings"><h2 class="h2">Basic Workings</h2></a>
			<ol class="list">
				<li>
					<span class="mb-auto">1.</span>
					<span class="flex flex-auto">
						<p>
							Request gem information and experience for leveling from <a
								href="https://poedb.tw/us"
								target="_blank"
								class="anchor inline-flex flex-row items-center mx-1"
								><img src="https://poe.ninja/shared-assets/ninja-logo.png" alt="" class="w-4 h-4" />
								poedb.tw</a
							>.
						</p>
					</span>
				</li>
				<li>
					<span class="mb-auto">2.</span>
					<span class="flex flex-auto">
						<p>
							Request new gem prices every 30min from <a
								href="https://poe.ninja"
								target="_blank"
								class="anchor inline-flex flex-row items-center mx-1"
								><img src="https://poedb.tw/favicon.ico" alt="" class="w-4 h-4" />poe.ninja</a
							>.
						</p></span
					>
				</li>
				<li>
					<span class="mb-auto">3.</span>
					<span class="flex flex-auto">
						<p>Filter prices by the minimum number of listerings, by default 20.</p>
					</span>
				</li>
				<li>
					<span class="mb-auto">4.</span>
					<span class="flex flex-auto">
						<p>Filter prices by the minimum experience for leveling, by default 300M.</p>
					</span>
				</li>
				<li>
					<span class="mb-auto">5.</span>
					<span class="flex flex-auto">
						<p>
							Calculate keyfigures by evaluate each recipe against each gem, if the required prices
							are available - havn't been filtered out.
						</p>
					</span>
				</li>
				<li>
					<span class="mb-auto">7.</span>
					<span class="flex flex-auto">
						<p>Sort gems by the keyfigure gain margin and present the gems.</p>
					</span>
				</li>
			</ol>
		</div>
		<div class="card variant-filled w-full p-4 shadow-xl">
			<a href="#why-is-gem-X-missing"><h2 class="h2">Why is gem X missing?</h2></a>
			<p>
				A missing gem is unable to bought as well as sold at the "Minimum listing count" specified
				in settings.<br />Often some level and quality comination is missing on
				<a
					href="https://poe.ninja"
					target="_blank"
					class="anchor inline-flex flex-row items-center mx-1"
					><img src="https://poedb.tw/favicon.ico" alt="" class="w-4 h-4" />poe.ninja</a
				>
				recipes depending on this specific variant, e.g. lvl21/23q wont evaluate at all.
				<br />
				Consequently a few niche gem combinations in low volume may be missed by GemStacker. Conservative
				estimates are preferable to false positives for high volatility listings, because leveling takes
				time, these opportunities are likely to be missed anyway.
			</p>
		</div>
		<div class="card w-full p-4 shadow-xl">
			<a href="#what-are-recipe-keyfigures"><h2 class="h2">What are recipe keyfigures?</h2></a>
			<p>
				Recipes calculate keyfigures on a per gem basis. Each recipe first verifies all prices
				required are present. If that fails the recipe does not calculate its keyfigures.
			</p>
			<ul class="list">
				<li>
					<span class="mb-auto">1.</span>
					<span class="flex flex-auto">
						<p>
							<strong>Adjusted earnings:</strong> the profit made by selling the gem, subtracting
							the cost of the recipe. <br /> Probabilistic methods present the average value
							accounting for the average loss of each possible outcome.
							<span class="text-primary-500-400-token mx-1 inline-flex flex-row">
								{wellKnownExchangeRateDisplay.gemcutters_prism.name}
								<img
									src={wellKnownExchangeRateDisplay.gemcutters_prism.img}
									alt={wellKnownExchangeRateDisplay.gemcutters_prism.img}
									class="h-4 w-4 mt-auto mb-[0.125rem]"
								/>
							</span> are most commonly used.
						</p>
					</span>
				</li>
				<li>
					<span class="mb-auto">3.</span>
					<span class="flex flex-auto">
						<p>
							<strong>Experience delta:</strong> The experience needing to be earned in order to
							level the gem up to max level. <br /> Probabilisitic methods present the average value
							accounting for level loss by corruption.
						</p></span
					>
				</li>
				<li>
					<span class="mb-auto">2.</span>
					<span class="flex flex-auto">
						<p>
							<strong>Attempts to profit:</strong> how many attempts are required for at least a 60%
							expectation of significat profit.
							<br /> Normal leveling guarantees profit, the recipe always has exactly 1 attempt
							until profit.
							<br />
							Probabilisitic methods, including
							<span class="text-primary-500-400-token mx-1 inline-flex flex-row">
								{wellKnownExchangeRateDisplay.vaal_orb.name}
								<img
									src={wellKnownExchangeRateDisplay.vaal_orb.img}
									alt={wellKnownExchangeRateDisplay.vaal_orb.img}
									class="h-4 w-4 mt-auto mb-[0.125rem]"
								/>
							</span>
							or similar randomizing factors, may have values greater than one. They calculate number
							of attempts for the 60% expectation by summing up the chance of every positive outcome
							weighted by how close they are to the best possible outcome in regards to profit.
						</p></span
					>
				</li>
				<li>
					<span class="mb-auto">4.</span>
					<span class="flex flex-auto">
						<p>
							<strong>Gain margin:</strong> The factor between the adjusted earnings and the experience
							delta multiplied by 1 million.
						</p>
					</span>
				</li>
			</ul>
		</div>
		<div class="card w-full p-4 shadow-xl">
			<a href="#which-recipes-are-available"><h2 class="h2">Which recipes are available?</h2></a>
			<p>
				The available recipes cover every known profitable strategy of leveling a gem.
				<br />
				Many recipes have equivalents for purchasing the gem from a vendor, for essencially free, and
				buying from the market. In case a gem can be bought from the vendor, the market buy is not evaluates,
				because it is always more expensive.
			</p>
			{#each gemProfitResponseItemRecipeName as key}
				{@const recipe = getRecipeInfo(key)}
				<a href="#recipe-{key}"><h3 class="h3 mt-2">{recipe.title}</h3></a>
				<p>
					{@html recipe.description.replaceAll('\n', '<br>')}
				</p>
			{/each}
		</div>
	</div>
</div>
