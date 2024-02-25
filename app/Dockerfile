FROM node:hydrogen-alpine AS builder
WORKDIR /app
COPY package*.json .
RUN npm ci

COPY . .
RUN npx svelte-kit sync
RUN npm run build --omit=dev
RUN npm prune --omit=dev

FROM node:hydrogen-alpine
WORKDIR /app
COPY .env .
COPY .env.* .
COPY --from=builder /app/build build
COPY --from=builder /app/node_modules node_modules/
COPY package.json .
EXPOSE 3000
ENV NODE_ENV=production
CMD [ "node", "build" ]