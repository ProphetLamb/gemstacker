#!/usr/bin/env sh
# -*- coding: utf-8 -*-
uvicorn --factory main:app --host "${HOST}" --port "${PORT}" --workers "${WORKERS}" --log-level "${LOG_LEVEL}"
