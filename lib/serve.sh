#!/usr/bin/env sh
# -*- coding: utf-8 -*-
pythion -m venv .venv
.venv/bin/pip3 install -r requirements.txt
.venv/bin/python3 -m uvicorn -- --factory get_listings:server --host
