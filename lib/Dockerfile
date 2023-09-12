FROM python:alpine AS compiletime
WORKDIR /app/
ENV PYTHONUNBUFFERED 1

RUN python -m venv /opt/venv
ENV PATH="/opt/venv/bin:$PATH"

COPY ./requirements.txt /app/requirements.txt
RUN pip install -r requirements.txt

FROM python:alpine AS runtime
WORKDIR /app/
COPY --from=compiletime /opt/venv /opt/venv

ENV PATH="/opt/venv/bin:$PATH"
COPY . .

ENV HOST="127.0.0.1"
ENV PORT="8000"
ENV DEBUG="False"
ENV WORKERS="4"
ENV LOG_LEVEL="info"
ENV ALLOWED_ORIGINS="*"
ENV BEARER_TOKEN=""
CMD ["uvicorn", "--factory", "main:app", "--host", "${HOST}", "--port", "${PORT}", "--workers", "${WORKERS}", "--log-level", "${LOG_LEVEL}"]
