#!/usr/bin/env bash
# Verificacion end-to-end contra Supabase real.
#   1. Aplica las migraciones pendientes.
#   2. Arranca la API y siembra el usuario administrador.
#   3. Ejecuta un login real y usa el token en un endpoint protegido.
# Nunca imprime la contrasena ni la cadena de conexion completa.
set -uo pipefail

RAIZ="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$RAIZ"

if [ ! -f .env ]; then
  echo "ERROR: no existe .env"; exit 1
fi

if grep -q "PEGA_AQUI_TU_PASSWORD" .env; then
  echo "ERROR: falta reemplazar <PEGA_AQUI_TU_PASSWORD> en .env"; exit 1
fi

# Carga .env sin volcarlo a la salida.
set -a
# shellcheck disable=SC1091
source .env
set +a

PUERTO=5080
BASE="http://localhost:$PUERTO"
ok=0; fallos=0
paso() { echo "  OK   $1"; ok=$((ok+1)); }
falla() { echo "  FALLA $1"; fallos=$((fallos+1)); }

echo "== 1. Migraciones =="
if dotnet ef database update \
    --project backend/src/Contratos.Infrastructure \
    --startup-project backend/src/Contratos.Api >/tmp/migraciones.log 2>&1; then
  paso "migraciones aplicadas"
else
  falla "migraciones"; tail -5 /tmp/migraciones.log
fi

echo "== 2. Arranque de la API =="
ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS="$BASE" \
  dotnet run --project backend/src/Contratos.Api --no-launch-profile \
  >/tmp/api-supabase.log 2>&1 &
API_PID=$!
trap 'kill $API_PID 2>/dev/null' EXIT

for _ in $(seq 1 60); do
  if curl -s -m 2 "$BASE/health" >/dev/null 2>&1; then break; fi
  sleep 1
done

if curl -s -m 3 "$BASE/health" | grep -q '"status":"ok"'; then
  paso "/health responde"
else
  falla "la API no arranco"; tail -15 /tmp/api-supabase.log; exit 1
fi

if grep -qE "Usuario administrador .* creado|el usuario .* ya existe" /tmp/api-supabase.log; then
  paso "seed del usuario administrador ejecutado"
else
  falla "seed no ejecutado"
fi

echo "== 3. Login real contra Supabase =="
RESP=$(curl -s -m 10 -X POST "$BASE/api/auth/login" \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"$Seed__AdminUsername\",\"password\":\"$Seed__AdminPassword\"}")

TOKEN=$(echo "$RESP" | python -c "import sys,json;print(json.load(sys.stdin).get('token',''))" 2>/dev/null)

if [ -n "$TOKEN" ]; then
  paso "login correcto (token de ${#TOKEN} caracteres)"
else
  falla "login"; echo "  respuesta: $RESP"
fi

CODIGO=$(curl -s -o /dev/null -w "%{http_code}" -m 10 -X POST "$BASE/api/auth/login" \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"$Seed__AdminUsername\",\"password\":\"password-incorrecta\"}")
[ "$CODIGO" = "401" ] && paso "password incorrecta -> 401" || falla "esperaba 401, recibi $CODIGO"

echo "== 4. Endpoint protegido =="
CODIGO=$(curl -s -o /dev/null -w "%{http_code}" -m 10 "$BASE/api/auth/me")
[ "$CODIGO" = "401" ] && paso "sin token -> 401" || falla "esperaba 401, recibi $CODIGO"

if [ -n "$TOKEN" ]; then
  CODIGO=$(curl -s -o /dev/null -w "%{http_code}" -m 10 "$BASE/api/auth/me" \
    -H "Authorization: Bearer $TOKEN")
  [ "$CODIGO" = "200" ] && paso "con token -> 200" || falla "esperaba 200, recibi $CODIGO"
fi

echo
echo "Resultado: $ok correctas, $fallos fallidas"
[ "$fallos" -eq 0 ] || exit 1
