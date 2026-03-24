FROM node:22-alpine AS build
WORKDIR /app

COPY Front/package*.json ./
RUN npm install

COPY Front/. .
RUN npm run build -- --configuration docker

FROM nginx:1.27-alpine AS runtime

COPY Front/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /tmp/dist

RUN set -eux; \
    mkdir -p /usr/share/nginx/html; \
    if [ -d /tmp/dist/front/browser ]; then cp -r /tmp/dist/front/browser/* /usr/share/nginx/html/; \
    elif [ -d /tmp/dist/Front/browser ]; then cp -r /tmp/dist/Front/browser/* /usr/share/nginx/html/; \
    elif [ -d /tmp/dist/front ]; then cp -r /tmp/dist/front/* /usr/share/nginx/html/; \
    elif [ -d /tmp/dist/Front ]; then cp -r /tmp/dist/Front/* /usr/share/nginx/html/; \
    else cp -r /tmp/dist/* /usr/share/nginx/html/; fi; \
    rm -rf /tmp/dist

EXPOSE 80
