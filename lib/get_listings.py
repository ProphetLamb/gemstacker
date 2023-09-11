from dataclasses import dataclass
import re
import time
import requests
import typing as t
import pandas as pd
import json
from datetime import datetime, timedelta

from get_experience import GemLevelProvider

@dataclass
class Options:
  pass 

def get_listings() -> list:
  # load json https://poe.ninja/api/data/itemoverview?league=Crucible&type=SkillGem&language=en
  # format:
  # {
  #   "lines": [
  #     {"id":96951,"name":"Awakened Enlighten Support","icon":"https://web.poecdn.com/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvR2Vtcy9TdXBwb3J0L1N1cHBvcnRQbHVzL0VubGlnaHRlbnBsdXMiLCJ3IjoxLCJoIjoxLCJzY2FsZSI6MX1d/7ec7d0544d/Enlightenplus.png","levelRequired":80,"variant":"5/23c","itemClass":4,"sparkline":{"data":[],"totalChange":0},"lowConfidenceSparkline":{"data":[0,0,9.56,9.56,63.25,83.88,47.33],"totalChange":47.33},"implicitModifiers":[],"explicitModifiers":[{"text":"This Gem gains 115% increased Experience","optional":false}],"flavourText":"","corrupted":true,"gemLevel":5,"gemQuality":23,"chaosValue":69159.71,"exaltedValue":4749.98,"divineValue":309.29,"count":3,"detailsId":"awakened-enlighten-support-5-23c","listingCount":3}
  #  ],
  #  "language":{"name":"en","translations":{}}
  #}
  content = requests.get('https://poe.ninja/api/data/itemoverview?league=Crucible&type=SkillGem&language=en')
  json = content.json()
  return json['lines']

def normalize_listings(listings: list) -> list:
  def filter_line(listing_entry: dict) -> bool:
  # if corrupted does not exist, or is not True
    if 'corrupted' in listing_entry:
      return listing_entry['corrupted'] != True
    if 'gemQuality' not in listing_entry:
      listing_entry['gemQuality'] = 0
    # count > 2
    if listing_entry['count'] <= 2:
      return False
    
    listing_entry['name'] = remove_variant(listing_entry['name'])
    return True
  return list(filter(filter_line, listings))

def remove_variant(gem_name: str) -> str:
  # remove quality prefixes
  if gem_name.startswith('Anomalous '):
    gem_name = gem_name[len('Anomalous '):]
  if gem_name.startswith('Divergent '):
    gem_name = gem_name[len('Divergent '):]
  if gem_name.startswith('Phantasmal '):
    gem_name = gem_name[len('Phantasmal '):]
  # remove gem level/quality suffixes (e.g. "5/20") find the pattern "( \d+/\d+.*)$" and remove it
  gem_name = re.sub(r'( \d+/\d+.*)$', '', gem_name)
  return gem_name

def get_gem_groups(listings: list) -> dict:
  # group all lines by name and quality
  grouped = {}
  for line in listings:
    name = line['name']
    quality = line['gemQuality']
    level = line['gemLevel']
    if name not in grouped:
      grouped[name] = {}
    if quality not in grouped[name]:
      grouped[name][quality] = {}
    grouped[name][quality][level] = line
  return grouped

class GemTradeUrlProvider:
  def __init__(self) -> None:
    self.last_request_time: t.Tuple[datetime, None] = None
    self.next_request_delta = timedelta(seconds=2)
    self.league: t.Tuple[str, None] = None
  
  def _sleep_on_request_if_needed(self):
    last_time = self.last_request_time
    now_time = datetime.now()
    if last_time is None:
      self.last_request_time = now_time
      return
    delta_time = now_time - last_time
    if last_time is not None and delta_time < self.next_request_delta:
      time.sleep((self.next_request_delta - delta_time).total_seconds())
    self.last_request_time = now_time

  def _get_default_headers(self) -> dict:
    return {
      'User-Agent': 'OAuth poe-gemleveling-profit-calculator/0.1 (contact: prophet.lamb@gmail.com)',
      'Accept': 'application/json'
    }

  def _get_current_league(self) -> str:
    if self.league is not None:
      return self.league
    self._sleep_on_request_if_needed()
    content = requests.get('https://www.pathofexile.com/api/trade/data/leagues', headers=self._get_default_headers()
    )
    json = content.json()
    df = pd.DataFrame(json['result'], columns=['id', 'realm', 'text'])
    df = df[df['realm'] == 'pc']
    df = df[~df['text'].str.contains('Standard')]
    # get the shortest league name
    df = df.sort_values(by=['text'], key=lambda x: x.str.len())
    self.league = df.iloc[0]['id']
    return self.league
    
  def make_gem_trade_url(self, gem_name: str, level: int) -> str:
    self._sleep_on_request_if_needed()
    league = self._get_current_league()
    url = f'https://www.pathofexile.com/api/trade/search/{league}'
    body = {
      "query": {
        "filters": {
          "misc_filters": {
            "filters": {
              "gem_level": {
                "min": level
              },
              "corrupted": {
                "option": "false"
              }
            }
          },
          "trade_filters": {
            "filters": {
              "collapse": {
                "option": "true"
              }
            }
          }
        },
        "status": {
          "option": "online"
        },
        "stats": [
          {
            "type": "and",
            "filters": []
          }
        ],
        "type":	gem_name
      },
      "sort": {
        "price": "asc"
      }
    }
    response = requests.post(url, json=body, headers=self._get_default_headers())
    json = response.json()
    return f'https://www.pathofexile.com/trade/search/{league}?{json["id"]}'
  

class GemListingProvider:
  def __init__(self) -> None:
    self.gem_level_provider = GemLevelProvider()


  def _get_gem_experience(self, listing: dict, level: int) -> float:
    #name = remove_variant(listing['name'])
    name = listing['name']
    level_data = self.gem_level_provider.get_level_data(name, level)
    if level_data is None:
      raise Exception(f'Could not find gem {listing["name"]} level {level}')
    return level_data['Total Experience']

  def _calculate_gain_margin(self, listing: dict, min_level: int, min_price: float, max_level: int, max_price: float) -> float:
    if (min_level == max_level):
      return 0
    # the game margin is chaos value / gem experience
    min_exp = self._get_gem_experience(listing, min_level)
    max_exp = self._get_gem_experience(listing, max_level)
    delta_exp = max_exp - min_exp
    if delta_exp == 0:
      return 0
    delta_price = max_price - min_price
    return delta_price * 1000000 / delta_exp

  def _group_price_chaos_delta(self, gem_groups: dict) -> dict:
    # read all items in the dict and build a new dict with the price for each gem level
    # "chaosValue":69159.71,"exaltedValue":4749.98,"divineValue":309.29
    prices = {}
    for name in gem_groups:
      for quality in gem_groups[name]:
        price_values = []
        for level in gem_groups[name][quality]:
          line = gem_groups[name][quality][level]
          price_values.append({'level': level, 'value': line['chaosValue'], 'listing': line})
        mind = None
        maxd = None
        if name in prices:
          mind = prices[name]['min']
          maxd = prices[name]['max']
        for price_data in price_values:
          if mind is None or price_data['value'] <= mind['price']:
            mind = { 'price': price_data['value'], 'level': price_data['level'] }
          if maxd is None or price_data['value'] >= maxd['price']:
            maxd = { 'price': price_data['value'], 'level': price_data['level'] }
        gain_margin = self._calculate_gain_margin(price_data['listing'], mind['level'], mind['price'], maxd['level'], maxd['price'])
        prices[name] = { 'min': mind, 'max': maxd, 'gain_margin': gain_margin }
    return prices
  
  def get_gem_groups_price_chaos_delta(self) -> dict:
    listings = get_listings()
    listings = normalize_listings(listings)
    gem_groups = get_gem_groups(listings)
    return self._group_price_chaos_delta(gem_groups)

def main():
  listing_provider = GemListingProvider()
  prices = listing_provider.get_gem_groups_price_chaos_delta()
  # order by gain margin
  prices = {k: v for k, v in sorted(prices.items(), key=lambda item: -item[1]['gain_margin'])}
  # where gem name starts with Awakened
  prices = {k: v for k, v in prices.items() if k.startswith('Awakened')}
  # take top 10
  prices = dict(list(prices.items())[0:5])
  trade_url_provider = GemTradeUrlProvider()
  for gem_name in prices:
    prices[gem_name]['min']['trade_url'] = trade_url_provider.make_gem_trade_url(gem_name, prices[gem_name]['min']['level'])
  print(json.dumps(prices, indent=2))

if __name__ == '__main__':
  main()
