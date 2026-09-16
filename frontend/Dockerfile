# syntax=docker/dockerfile:1
#
# Aplicacion Angular servida por nginx. Contexto de construccion: carpeta frontend/.
#   docker build -t contratos-web ./frontend
#
# nginx sirve la aplicacion y reenvia /api a la API. Frontend y API comparten
# origen: no hace falta CORS y la imagen no depende del dominio donde se publique.

# ---- Compilacion ------------------------------------------------------------
FROM node:22-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci --no-audit --no-fund

COPY . .
RUN npx ng build --configuration production

# ---- Ejecucion --------------------------------------------------------------
# Variante sin privilegios: nginx no corre como root y escucha en el 8080.
FROM nginxinc/nginx-unprivileged:1.29-alpine AS runtime

# Plantilla procesada con envsubst al arrancar: permite apuntar a otra API
# (API_UPSTREAM) sin reconstruir la imagen.
ENV API_UPSTREAM=http://api:8080
COPY nginx/default.conf.template /etc/nginx/templates/default.conf.template

COPY --from=build /app/dist/contratos-app/browser /usr/share/nginx/html

EXPOSE 8080

HEALTHCHECK --interval=15s --timeout=5s --start-period=10s --retries=3 \
    CMD wget -q --spider http://127.0.0.1:8080/ || exit 1
