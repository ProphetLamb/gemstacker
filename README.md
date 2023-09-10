# Gem leveling profit calculator

Computes the profit gained per experience invested in a gem.
Useful to make a net profit on 5-way rotas, and similar mechanics.

## Usage

```json
{
  "Awakened Elemental Damage with Attacks Support": {
    "min": {
      "price": 403.03,
      "level": 3,
      "trade_url": "https://www.pathofexile.com/trade/search/Ancestor?YDoJ52ghY"
    },
    "max": {
      "price": 810.3,
      "level": 5
    },
    "gain_margin": 0.5957181237539187
  },
  "Awakened Enhance Support": {
    "min": {
      "price": 9206.01,
      "level": 1,
      "trade_url": "https://www.pathofexile.com/trade/search/Ancestor?80dZvaZFV"
    },
    "max": {
      "price": 10266.61,
      "level": 4
    },
    "gain_margin": 0.5082111309696633
  },
}
  ```

## Function

Checks [poe.ninja](https://poe.ninja/) for the prices of different gem levels.

Checks [poedb.tw](https://poedb.tw/us) for the experience needed to level gems.

Generates [poe trade](https://www.pathofexile.com/trade/search/) urls to buy the gems.

Computes the `gain_margin` amount of profit in Chaos made by buying the non-corrupted gem at the level which is cheapest and selling at the highest per 1000000 experience required to make up the difference in levels.
For example level 3 'Awakened Elemental Damage with Attacks Support' is 403 chaos. A level 5 is 810 chaos.
We buy for 403 leech in 5-ways and sell for 810 divines, for a total of 407 chaos in profit.
This profit is now correlated to how many rotas we need to keep this gem by dividing by the experience needed.
For human readability per 1000000 experience.

$$ \verb|gain_margin|_{3,5}(\verb|Awakened Elemental Damage with Attacks Support|) := \frac{810\verb|c| - 403\verb|c|}{683662262\verb|exp|} * 1000000 = 0.595 $$

## Implementation

- Scraping and requests have rate limits. Initially a cache of all gem info from [poedb.tw](https://poedb.tw/us) is built. This will take few minutes.
- Generating [poe trade](https://www.pathofexile.com/trade/search/) urls requires a significant rate limit. This will also take time.
