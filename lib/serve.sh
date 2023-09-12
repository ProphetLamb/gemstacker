#!/usr/bin/env sh
# -*- coding: utf-8 -*-
python3 -m venv .venv
.venv/bin/pip3 install -r requirements.txt
.venv/bin/python3 -m uvicorn --factory main:app --host 127.0.0.1 --port 8000 --reload
