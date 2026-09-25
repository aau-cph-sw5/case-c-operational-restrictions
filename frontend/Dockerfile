# Stage 1: Dev
FROM node:22-alpine AS dev
WORKDIR /app

RUN npm install -g pnpm@12.4.2

COPY package.json pnpm-lock.yaml pnpm-workspace.yaml ./
COPY frontend/package.json ./frontend/package.json

RUN pnpm install --frozen-lockfile

COPY frontend ./frontend

ENV CHOKIDAR_USEPOLLING=true
EXPOSE 5173

CMD ["pnpm", "--filter", "frontend", "dev"]

# Stage 2: Build React/Vite static assets
FROM dev AS build
RUN pnpm --filter frontend build

# Stage 3: Serve with Nginx (Production)
FROM nginx:alpine AS runtime
COPY --from=build /app/frontend/dist /usr/share/nginx/html
COPY frontend/nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
