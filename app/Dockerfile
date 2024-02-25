FROM node:hydrogen-alpine AS builder
WORKDIR /app
COPY package*.json .
RUN npm ci

COPY . .
RUN npm install -g dotenv-cli
RUN dotenv -- npx svelte-kit sync
RUN dotenv -- npm run build --omit=dev
RUN dotenv -- npm prune --omit=dev

FROM node:hydrogen-alpine
WORKDIR /app
RUN npm install -g dotenv-cli
COPY .env .
COPY .env.* .
COPY --from=builder /app/build build
COPY --from=builder /app/node_modules node_modules/
COPY package.json .
EXPOSE 3000
ENV NODE_ENV=production
CMD [ "dotenv", "--", "node", "build" ]