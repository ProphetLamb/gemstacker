#!/usr/bin/env sh
# -*- coding: utf-8 -*-
python3 -m venv .venv
.venv/bin/pip3 install -r requirements.txt

export DEBUG="true"
export WORKERS="4"
export LOG_LEVEL="debug"
export ALLOWED_ORIGINS="*"
export BEARER_TOKEN="test"
.venv/bin/python3 -m uvicorn --factory main:app --host 127.0.0.1 --port 8000 --reload
