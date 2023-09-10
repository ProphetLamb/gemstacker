from dataclasses import dataclass
import sys
import os
import time
import requests
from bs4 import BeautifulSoup
import typing as t
import pandas as pd
import json
from datetime import datetime, timedelta

POEDB_BASE_URL = 'https://poedb.tw/us'

def get_gem_level_table(gem_name: str) -> t.Dict[int, dict]:
  # query https://poedb.tw/us/{gem_name} with spaces replaced by underscores
  page = requests.get(f'{POEDB_BASE_URL}/{gem_name.replace(" ", "_")}')
  # get the last element table at .tab-pane>.card>.table-responsive>.table
  soup = BeautifulSoup(page.content, 'html.parser')
  level_effect = soup.select('.tab-pane>.card>.card-header')
  level_effect = next(filter(lambda heading: heading.text.startswith('Level Effect'), level_effect), None)
  # find sibling div.table-responsive
  table = level_effect.find_next_sibling('div', class_='table-responsive')
  table = table.find('table')
  # read the table into a pandas dataframe
  df = pd.read_html(str(table))[0]
  df = df.fillna(0)
  df['Level'] = df['Level'].astype(int)
  # form into dict of 'Level' -> ...columns
  return df.set_index('Level').to_dict(orient='index')

class GemLevelProvider:
  def __init__(self, cache_directory: t.Tuple[str, None] = None, rate_limiter_timeout_seconds: int = 1) -> None:
    self.known_gems: t.Dict[str, dict] = {}
    self.cache_directory: str = cache_directory if cache_directory else '.cache/gem_experience'
    self.last_request_time: t.Tuple[datetime, None] = None
    self.next_request_delta = timedelta(seconds=rate_limiter_timeout_seconds)

  def get_level_data(self, gem_name: str, level: int) -> t.Tuple[dict, None]:
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
  
  def get_levels_table(self, gem_name: str) -> t.Tuple[t.Dict[int, dict], None]:
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

  def _get_or_create_table(self, gem_name: str) -> t.Tuple[t.Dict[int, dict], None]:
    if gem_name not in self.known_gems:
      self.load_cache(gem_name)
    if gem_name not in self.known_gems:
      self._sleep_on_request_if_needed()
      try:
        self.known_gems[gem_name] = get_gem_level_table(gem_name)
      except:
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

  def save_cache(self, gem_name: t.Tuple[str, None] = None):
    self._ensure_cache_dir()
    if gem_name is None:
      for gem_name in self.known_gems:
        if gem_name is None:
          continue
        self.save_cache(gem_name)
      return
    with open(f'{self.cache_directory}/{gem_name}.json', 'w') as f:
      json.dump(self.known_gems[gem_name], f, indent=2)

if __name__ == "__main__":
  table = get_gem_level_table('Awakened Enlighten Support')
  print(json.dumps(table, indent=2))