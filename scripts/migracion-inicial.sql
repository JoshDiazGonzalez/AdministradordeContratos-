CREATE TABLE IF NOT EXISTS public.__ef_migrations_history (
    migration_id character varying(150) NOT NULL,
    product_version character varying(32) NOT NULL,
    CONSTRAINT pk___ef_migrations_history PRIMARY KEY (migration_id)
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "migration_id" = '20260916022420_InitialCreate') THEN
    CREATE TABLE contratos (
        id uuid NOT NULL,
        nombre_proveedor character varying(200) NOT NULL,
        monto_contrato numeric(18,2) NOT NULL,
        fecha_inicio date NOT NULL,
        fecha_vencimiento date NOT NULL,
        inactivo boolean NOT NULL DEFAULT FALSE,
        descripcion character varying(1000) NOT NULL,
        archivo_nombre character varying(255) NOT NULL,
        archivo_ruta character varying(500) NOT NULL,
        archivo_content_type character varying(150) NOT NULL,
        archivo_tamano_bytes bigint NOT NULL,
        fecha_creacion timestamptz NOT NULL,
        fecha_actualizacion timestamptz,
        CONSTRAINT pk_contratos PRIMARY KEY (id),
        CONSTRAINT ck_contratos_archivo_tamano CHECK (archivo_tamano_bytes > 0),
        CONSTRAINT ck_contratos_monto_positivo CHECK (monto_contrato > 0),
        CONSTRAINT ck_contratos_rango_fechas CHECK (fecha_vencimiento >= fecha_inicio)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "migration_id" = '20260916022420_InitialCreate') THEN
    CREATE TABLE usuarios (
        id uuid NOT NULL,
        username character varying(50) NOT NULL,
        password_hash character varying(255) NOT NULL,
        nombre_completo character varying(150) NOT NULL,
        fecha_creacion timestamptz NOT NULL,
        CONSTRAINT pk_usuarios PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "migration_id" = '20260916022420_InitialCreate') THEN
    CREATE INDEX ix_contratos_fecha_inicio ON contratos (fecha_inicio);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "migration_id" = '20260916022420_InitialCreate') THEN
    CREATE INDEX ix_contratos_inactivo_fecha_vencimiento ON contratos (inactivo, fecha_vencimiento);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "migration_id" = '20260916022420_InitialCreate') THEN
    CREATE INDEX ix_contratos_nombre_proveedor ON contratos (nombre_proveedor);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "migration_id" = '20260916022420_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_usuarios_username ON usuarios (username);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "migration_id" = '20260916022420_InitialCreate') THEN
    INSERT INTO public.__ef_migrations_history (migration_id, product_version)
    VALUES ('20260916022420_InitialCreate', '10.0.12');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "migration_id" = '20260916022451_HabilitarRowLevelSecurity') THEN
    ALTER TABLE public.contratos ENABLE ROW LEVEL SECURITY;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "migration_id" = '20260916022451_HabilitarRowLevelSecurity') THEN
    ALTER TABLE public.usuarios ENABLE ROW LEVEL SECURITY;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "migration_id" = '20260916022451_HabilitarRowLevelSecurity') THEN
    REVOKE ALL ON public.contratos FROM anon, authenticated;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "migration_id" = '20260916022451_HabilitarRowLevelSecurity') THEN
    REVOKE ALL ON public.usuarios FROM anon, authenticated;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "migration_id" = '20260916022451_HabilitarRowLevelSecurity') THEN
    INSERT INTO public.__ef_migrations_history (migration_id, product_version)
    VALUES ('20260916022451_HabilitarRowLevelSecurity', '10.0.12');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "migration_id" = '20260916022641_ProtegerHistorialDeMigraciones') THEN
    ALTER TABLE public.__ef_migrations_history ENABLE ROW LEVEL SECURITY;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "migration_id" = '20260916022641_ProtegerHistorialDeMigraciones') THEN
    REVOKE ALL ON public.__ef_migrations_history FROM anon, authenticated;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM public.__ef_migrations_history WHERE "migration_id" = '20260916022641_ProtegerHistorialDeMigraciones') THEN
    INSERT INTO public.__ef_migrations_history (migration_id, product_version)
    VALUES ('20260916022641_ProtegerHistorialDeMigraciones', '10.0.12');
    END IF;
END $EF$;
COMMIT;

