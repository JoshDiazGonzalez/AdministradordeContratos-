using Contratos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Contratos.Infrastructure.Persistence.Configurations;

public class ContratoConfiguration : IEntityTypeConfiguration<Contrato>
{
    public void Configure(EntityTypeBuilder<Contrato> builder)
    {
        builder.ToTable("contratos");

        builder.HasKey(c => c.Id);

        // El Id lo genera el dominio con UUIDv7 (ordenado por tiempo).
        builder.Property(c => c.Id)
            .ValueGeneratedNever();

        builder.Property(c => c.NombreProveedor)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.NombreProveedorBusqueda)
            .IsRequired()
            .HasMaxLength(200);

        // Dinero: numeric exacto. Nunca float ni double.
        builder.Property(c => c.MontoContrato)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(c => c.FechaInicio)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(c => c.FechaVencimiento)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(c => c.Inactivo)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(c => c.Descripcion)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(c => c.ArchivoNombre)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(c => c.ArchivoRuta)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(c => c.ArchivoContentType)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(c => c.ArchivoTamanoBytes)
            .IsRequired();

        // timestamptz: siempre con zona horaria, para no perder informacion.
        builder.Property(c => c.FechaCreacion)
            .IsRequired()
            .HasColumnType("timestamptz");

        builder.Property(c => c.FechaActualizacion)
            .HasColumnType("timestamptz");

        // Las reglas de negocio se garantizan tambien en la base: aunque alguien
        // inserte por SQL directo, no puede crear datos invalidos.
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_contratos_monto_positivo", "monto_contrato > 0");
            t.HasCheckConstraint("ck_contratos_rango_fechas", "fecha_vencimiento >= fecha_inicio");
            t.HasCheckConstraint("ck_contratos_archivo_tamano", "archivo_tamano_bytes > 0");
        });

        // La busqueda por proveedor se hace sobre la columna normalizada.
        builder.HasIndex(c => c.NombreProveedorBusqueda)
            .HasDatabaseName("ix_contratos_nombre_proveedor_busqueda");

        builder.HasIndex(c => c.FechaInicio)
            .HasDatabaseName("ix_contratos_fecha_inicio");

        // Indice compuesto que cubre el filtro por estado: el estado se traduce a
        // predicados sobre inactivo + fecha_vencimiento, nunca se filtra en memoria.
        builder.HasIndex(c => new { c.Inactivo, c.FechaVencimiento })
            .HasDatabaseName("ix_contratos_inactivo_fecha_vencimiento");
    }
}
