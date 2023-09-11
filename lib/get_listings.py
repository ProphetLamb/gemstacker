import asyncio
from dataclasses import dataclass
import io
import os
import re
import threading
import time
import requests
import typing as t
import pandas as pd
import json
import fnmatch
from datetime import datetime, timedelta
from bs4 import BeautifulSoup
from starlette.applications import Starlette
from starlette.responses import JSONResponse
from starlette.routing import Route
from starlette.requests import Request

class GemLevelProvider:
  def __init__(self, cache_directory: t.Optional[str] = None, rate_limiter_timeout_seconds: int = 1) -> None:
    self.known_gems: t.Dict[str, dict] = {}
    self.cache_directory: str = cache_directory if cache_directory else '.cache/gem_experience'
    self.last_request_time: t.Optional[datetime] = None
    self.next_request_delta = timedelta(seconds=rate_limiter_timeout_seconds)

  def get_gem_level_table(self, gem_name: str) -> t.Dict[int, dict]:
    # query https://poedb.tw/us/{gem_name} with spaces replaced by underscores
    page = requests.get(f'https://poedb.tw/us/{gem_name.replace(" ", "_")}')
    # get the last element table at .tab-pane>.card>.table-responsive>.table
    soup = BeautifulSoup(page.content, 'html.parser')
    level_effect = soup.select('.tab-pane>.card>.card-header')
    level_effect = next(filter(lambda heading: heading.text.startswith('Level Effect'), level_effect), None)
    # find sibling div.table-responsive
    table = level_effect.find_next_sibling('div', class_='table-responsive')
    table = table.find('table')
    # read the table into a pandas dataframe
    table_str = io.StringIO(str(table))
    df = pd.read_html(table_str)[0]
    df = df.fillna(0)
    df['Level'] = df['Level'].astype(int)
    # form into dict of 'Level' -> ...columns
    return df.set_index('Level').to_dict(orient='index')


  def get_level_data(self, gem_name: str, level: int) -> t.Optional[dict]:
    cache = self._get_or_create_table(gem_name)
    if cache is None:
      return None
    if level not in cache:
      return None
    # compute total experience to reach this level
    total_experience = 0
    for i in range(1, level):
      total_experience += cache[i]['Experience']
    total_experience = {'Total Experience': total_experience }

    return cache[level] | total_experience

  def get_levels_table(self, gem_name: str) -> t.Optional[t.Dict[int, dict]]:
    cache = self._get_or_create_table(gem_name)
    return dict(cache) if cache is not None else None

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

  def _get_or_create_table(self, gem_name: str) -> t.Optional[t.Dict[int, dict]]:
    if gem_name not in self.known_gems:
      self.load_cache(gem_name)
    if gem_name not in self.known_gems:
      self._sleep_on_request_if_needed()
      try:
        self.known_gems[gem_name] = self.get_gem_level_table(gem_name)
      except Exception as e:
        self.known_gems[gem_name] = None
        return None
      self.save_cache(gem_name)
    return self.known_gems[gem_name]

  def _ensure_cache_dir(self):
    if not os.path.exists(self.cache_directory):
      os.makedirs(self.cache_directory)

  def load_cache(self, gem_name: str):
    self._ensure_cache_dir()
    filename = f'{gem_name}.json'
    if not os.path.exists(f'{self.cache_directory}/{filename}'):
      return
    with open(f'{self.cache_directory}/{filename}') as f:
      loaded = json.load(f)
      # convert all keys to int
      self.known_gems[gem_name] = {int(k): v for k, v in loaded.items()}

  def clear_cache(self):
    self._ensure_cache_dir()
    for filename in os.listdir(self.cache_directory):
      if not filename.endswith('.json'):
        continue
      os.remove(f'{self.cache_directory}/{filename}')
    self.known_gems = {}

  def save_cache(self, gem_name: t.Optional[str] = None):
    self._ensure_cache_dir()
    if gem_name is None:
      for gem_name in self.known_gems:
        if gem_name is None:
          continue
        self.save_cache(gem_name)
      return
    with open(f'{self.cache_directory}/{gem_name}.json', 'w') as f:
      json.dump(self.known_gems[gem_name], f, indent=2)

class GemTradeUrlProvider:
  def __init__(self) -> None:
    self.last_request_time: t.Optional[datetime] = None
    self.next_request_delta = timedelta(seconds=2)
    self.league: t.Optional[str] = None

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
    self.last_request_time: t.Optional[datetime] = None
    self.cache_time_to_live = timedelta(minutes=30)
    self.cached_data: t.Optional[dict] = None

  def _fetch_listings(self) -> list:
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

  def _normalize_listings(self, listings: list) -> list:
    def filter_line(listing_entry: dict) -> bool:
    # if corrupted does not exist, or is not True
      if 'corrupted' in listing_entry:
        return listing_entry['corrupted'] != True
      if 'gemQuality' not in listing_entry:
        listing_entry['gemQuality'] = 0
      # count > 2
      if listing_entry['count'] <= 2:
        return False

      listing_entry['name'] = self._remove_variant(listing_entry['name'])
      return True
    return list(filter(filter_line, listings))

  def _remove_variant(self, gem_name: str) -> str:
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

  def _get_gem_groups(self, listings: list) -> dict:
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

  def get_gem_groups(self) -> dict:
    now_time = datetime.now()
    if self.cached_data is not None and self.last_request_time is not None and now_time - self.last_request_time < self.cache_time_to_live:
      return self.cached_data
    self.last_request_time = now_time
    data = self._fetch_listings()
    data = self._normalize_listings(data)
    data = self._get_gem_groups(data)
    self.cached_data =  data
    return self.cached_data

class GemGainMarginProvider:
  def __init__(self, gem_level_provider: GemLevelProvider, gem_listing_provider: GemListingProvider) -> None:
    self.gem_level_provider = gem_level_provider
    self.gem_listing_provider = gem_listing_provider
    self.last_request_time: t.Optional[datetime] = None
    self.cache_time_to_live = timedelta(seconds=30)
    self.cached_data: t.Optional[dict] = None

  def _get_gem_experience(self, listing: dict, level: int) -> float:
    name = listing['name']
    level_data = self.gem_level_provider.get_level_data(name, level)
    if level_data is None:
      raise RuntimeError(f'Could not find gem {listing["name"]} level {level}')
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
    time_now = datetime.now()
    if self.cached_data is not None and self.last_request_time is not None and time_now - self.last_request_time < self.cache_time_to_live:
      return self.cached_data
    self.last_request_time = time_now
    gem_groups = self.gem_listing_provider.get_gem_groups()
    gem_groups = self._group_price_chaos_delta(gem_groups)
    self.cached_data = gem_groups
    return self.cached_data

@dataclass
class GemProfitRequestParameter:
  gem_name: str = "*"
  min_sell_price_chaos: t.Optional[int] = None
  max_buy_price_chaos: t.Optional[int] = None
  items_offset: int = 0
  items_count: int = 10

  def validate(self) -> t.Generator[str, None, None]:
    if self.gem_name is None or self.gem_name == '':
      self.gem_name = '*'
    if self.min_sell_price_chaos is not None and self.min_sell_price_chaos < 0:
      yield 'Min sell price must be greater than 0'
    if self.max_buy_price_chaos is not None and self.max_buy_price_chaos < 0:
      yield 'Max buy price must be greater than 0'
    if self.items_offset < 0:
      yield 'Items offset must be greater than 0'
    if self.items_count < 0 or self.items_count > 100:
      yield 'Items count must be between 0 and 100'

@dataclass
class GemProfitRequestResult:
  data: dict

class GemProfitRequestHandler:
  def __init__(self, gem_margin_provider: GemGainMarginProvider) -> None:
    self.gem_margin_provider = gem_margin_provider

  async def handle(self, parameters: GemProfitRequestParameter) -> GemProfitRequestResult:
    gem_groups = self.gem_margin_provider.get_gem_groups_price_chaos_delta()
    # apply filter to each entry
    gem_groups = {k: v for k, v in gem_groups.items() if self.filter_entry(k, v, parameters)}
    # sort by gain margin descending
    gem_groups = {k: v for k, v in sorted(gem_groups.items(), key=lambda item: -item[1]['gain_margin'])}
    # take only the first n items
    gem_groups = dict(list(gem_groups.items())[parameters.items_offset:parameters.items_offset + parameters.items_count])
    return GemProfitRequestResult(gem_groups)

  def filter_entry(self, gem_name: str, data: dict, parameters: GemProfitRequestParameter) -> bool:
    if not fnmatch.fnmatch(gem_name, parameters.gem_name):
      return False
    if parameters.min_sell_price_chaos is not None and data['min']['price'] < parameters.min_sell_price_chaos:
      return False
    if parameters.max_buy_price_chaos is not None and data['max']['price'] > parameters.max_buy_price_chaos:
      return False
    return True

def test():
  gem_level_provider = GemLevelProvider()
  gem_listing_provider = GemListingProvider()
  gem_margin_provider = GemGainMarginProvider(gem_level_provider, gem_listing_provider)
  request_parameter = GemProfitRequestParameter(gem_name='Awakened*', min_sell_price_chaos=500, max_buy_price_chaos=400, items_offset=0, items_count=10)
  request_handler = GemProfitRequestHandler(gem_margin_provider)
  result = asyncio.run(request_handler.handle(request_parameter))
  # trade_url_provider = GemTradeUrlProvider()
  # for gem_name in result.data:
  #   result.data[gem_name]['min']['trade_url'] = trade_url_provider.make_gem_trade_url(gem_name, prices[gem_name]['min']['level'])
  print(json.dumps(result.data, indent=2))

def server() -> Starlette:
  gem_margin_provider = GemGainMarginProvider(GemLevelProvider(), GemListingProvider())
  gem_margin_provider_lock = threading.Lock()

  async def get_gem_profit(request: Request) -> JSONResponse:
    parameters = GemProfitRequestParameter(**request.query_params)
    errors = list(parameters.validate())
    if len(errors) > 0:
      return JSONResponse({'errors': errors}, status_code=400)
    with gem_margin_provider_lock:
      handler = GemProfitRequestHandler(gem_margin_provider)
      result = await handler.handle(parameters)
      return JSONResponse(result.data)

  routes = [
    Route('/gem-profit', get_gem_profit, methods=['GET'])
  ]
  app = Starlette(debug=False, routes=routes)
  return app

if __name__ == '__main__':
  test()
