#!/usr/bin/env bash
# Configura el proyecto contra el PostgreSQL instalado en esta maquina, para
# poder probar todo en local sin depender de Supabase ni de Docker.
#
#   bash scripts/usar-postgres-local.sh <password-de-postgres>
#
# Hace tres cosas:
#   1. Crea la base de datos si no existe.
#   2. Reescribe ConnectionStrings__DefaultConnection en .env.
#   3. Aplica las migraciones de EF Core.
#
# No cambia ni una linea de codigo: la cadena de conexion es configuracion.
# Para volver a Supabase basta con editar .env de nuevo.
set -uo pipefail

RAIZ="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$RAIZ"

PSQL="/c/Program Files/PostgreSQL/14/bin/psql"
BASE="contratos_dev"
USUARIO="postgres"
HOST="localhost"
PUERTO="5432"

if [ $# -lt 1 ]; then
  cat <<'AYUDA'
ERROR: falta la contrasena del usuario "postgres" de tu PostgreSQL local.

  bash scripts/usar-postgres-local.sh TU_PASSWORD

Es la contrasena que definiste al instalar PostgreSQL 14, no la de Supabase.
Si no la recuerdas, puedes restablecerla desde pgAdmin.

Alternativa: exportarla para que no quede en el historial del shell.
  export PGPASSWORD='...' && bash scripts/usar-postgres-local.sh "$PGPASSWORD"
AYUDA
  exit 1
fi

export PGPASSWORD="$1"

if [ ! -x "$PSQL" ]; then
  echo "ERROR: no se encontro psql en $PSQL"
  exit 1
fi

echo "== 1. Comprobando conexion =="
if ! "$PSQL" -h "$HOST" -p "$PUERTO" -U "$USUARIO" -d postgres -c "select 1" >/dev/null 2>&1; then
  echo "  FALLA: no se pudo conectar. Revisa la contrasena de postgres."
  exit 1
fi
echo "  OK   conectado a PostgreSQL local"

echo "== 2. Creando la base $BASE si no existe =="
EXISTE=$("$PSQL" -h "$HOST" -p "$PUERTO" -U "$USUARIO" -d postgres -tAc \
  "select 1 from pg_database where datname = '$BASE'")

if [ "$EXISTE" = "1" ]; then
  echo "  OK   ya existia"
else
  "$PSQL" -h "$HOST" -p "$PUERTO" -U "$USUARIO" -d postgres \
    -c "create database $BASE" >/dev/null && echo "  OK   creada"
fi

echo "== 3. Actualizando .env =="
# Sin "SSL Mode": Npgsql usa Prefer, que negocia TLS si el servidor lo ofrece.
# No se desactiva la validacion en ningun caso.
CADENA="Host=$HOST;Port=$PUERTO;Database=$BASE;Username=$USUARIO;Password=$1;Maximum Pool Size=20"

python - "$CADENA" <<'PY'
import sys, io, os

cadena = sys.argv[1]
ruta = '.env'
lineas = io.open(ruta, encoding='utf-8').read().split('\n')
salida, reemplazada = [], False

for l in lineas:
    if l.startswith('ConnectionStrings__DefaultConnection='):
        if not reemplazada:
            salida.append('# Cadena de Supabase (session pooler), comentada mientras se usa la base local:')
            salida.append('# ' + l)
            salida.append('ConnectionStrings__DefaultConnection=' + cadena)
            reemplazada = True
        continue
    if l.startswith('# ConnectionStrings__DefaultConnection=') or \
       l.startswith('# Cadena de Supabase'):
        continue
    salida.append(l)

if not reemplazada:
    salida.append('ConnectionStrings__DefaultConnection=' + cadena)

io.open(ruta, 'w', encoding='utf-8', newline='\n').write('\n'.join(salida))
print('  OK   .env apunta ahora a la base local')
PY

echo "== 4. Aplicando migraciones =="
if dotnet ef database update \
    --project backend/src/Contratos.Infrastructure \
    --startup-project backend/src/Contratos.Api >/tmp/migraciones-local.log 2>&1; then
  echo "  OK   migraciones aplicadas"
else
  echo "  FALLA: revisa /tmp/migraciones-local.log"
  tail -15 /tmp/migraciones-local.log
  exit 1
fi

echo
echo "Listo. Ya puedes pulsar F5 en Visual Studio o ejecutar:"
echo "  dotnet run --project backend/src/Contratos.Api"
echo
echo "Swagger:     http://localhost:5080/swagger"
echo "Credenciales: admin / Admin123*"
