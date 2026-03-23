FROM node:20-alpine AS build
WORKDIR /app

COPY frontend/clone-ebay-fe/package*.json ./
RUN npm install

COPY frontend/clone-ebay-fe ./
RUN npm run build

FROM nginx:alpine
COPY --from=build /app/dist /usr/share/nginx/html
COPY frontend/nginx.frontend.conf /etc/nginx/conf.d/default.conf
EXPOSE 80