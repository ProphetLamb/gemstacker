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

RUN chmod +x start.sh
CMD ["./start.sh"]
