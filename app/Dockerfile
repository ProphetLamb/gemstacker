FROM node:hydrogen-alpine AS builder
# build arguments
ARG SENTRY_AUTH_TOKEN
ARG API_KEY
ARG API_ENDPOINT

RUN apk update && apk upgrade && adduser -D svelteuser
USER svelteuser

WORKDIR /app
COPY --chown=svelteuser:svelteuser package*.json .
RUN npm ci

COPY --chown=svelteuser:svelteuser  . .
ENV SENTRY_AUTH_TOKEN=${SENTRY_AUTH_TOKEN} \
  API_KEY=${API_KEY} \
  API_ENDPOINT=${API_ENDPOINT}
RUN npx svelte-kit sync
RUN npm run build --omit=dev
RUN npm prune --omit=dev

FROM node:hydrogen-alpine
# node arguments
ARG NODE_ENV=production
ARG ORIGIN=localhost
ARG PROTOCOL_HEADER=x-forwarded-proto
ARG HOST_HEADER=x-forwarded-host
ARG ADDRESS_HEADER=True-Client-IP
ARG XFF_DEPTH=1

RUN apk update && apk upgrade && apk add dumb-init && adduser -D svelteuser
USER svelteuser

WORKDIR /app
COPY --chown=svelteuser:svelteuser --from=builder /app/build build
COPY --chown=svelteuser:svelteuser --from=builder /app/node_modules node_modules/
COPY package.json .
EXPOSE 3000
ENV NODE_ENV=${NODE_ENV} \
  ORIGIN=${ORIGIN} \
  PROTOCOL_HEADER=${PROTOCOL_HEADER} \
  HOST_HEADER=${HOST_HEADER} \
  ADDRESS_HEADER=${ADDRESS_HEADER} \
  XFF_DEPTH=${XFF_DEPTH}
CMD [ "node", "build" ]