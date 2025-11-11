FROM node:current-alpine AS builder
ARG API_KEY
ARG API_ENDPOINT
ENV API_KEY=${API_KEY} \
  API_ENDPOINT=${API_ENDPOINT}
WORKDIR /app
COPY package*.json .
RUN npm ci

COPY . .
RUN npm run build
RUN npm prune --production

FROM node:current-alpine
ARG NODE_ENV=production
ARG ORIGIN=localhost
ARG PROTOCOL_HEADER=x-forwarded-proto
ARG HOST_HEADER=x-forwarded-host
ARG ADDRESS_HEADER=True-Client-IP
ARG XFF_DEPTH=1
WORKDIR /app
COPY --from=builder /app/build build/
COPY --from=builder /app/node_modules node_modules/
COPY package.json .
EXPOSE 3000
ENV NODE_ENV=${NODE_ENV} \
  ORIGIN=${ORIGIN} \
  PROTOCOL_HEADER=${PROTOCOL_HEADER} \
  HOST_HEADER=${HOST_HEADER} \
  ADDRESS_HEADER=${ADDRESS_HEADER} \
  XFF_DEPTH=${XFF_DEPTH}
CMD [ "node", "build" ]