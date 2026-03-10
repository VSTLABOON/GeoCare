using GeoCare.Core.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GeoCare.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ─── DbSets ───────────────────────────────────────────────────────────────
    // Cada DbSet representa una tabla en SQL Server.
    public DbSet<Hospital> Hospitals { get; set; }
    public DbSet<Specialty> Specialties { get; set; }
    public DbSet<HospitalSpecialty> HospitalSpecialties { get; set; }
    public DbSet<MedicalResource> MedicalResources { get; set; }

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // ─── Conversión enum → string ─────────────────────────────────────────────
    // Almacenar HospitalType como VARCHAR en lugar de int tiene dos ventajas:
    //   1. Los datos son legibles directamente en SQL Server sin consultar el código.
    //   2. Agregar o reordenar valores del enum nunca corrompe datos existentes.
    //
    // "Private" ocupa 7 chars; "SectorSalud" 11 chars; 20 es suficiente con margen.
        modelBuilder.Entity<Hospital>()
            .Property(h => h.Type)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsUnicode(false);    // VARCHAR en lugar de NVARCHAR — los valores son ASCII puro

        // ─── Clave compuesta tabla pivote ─────────────────────────────────────────
        modelBuilder.Entity<HospitalSpecialty>()
            .HasKey(hs => new { hs.HospitalId, hs.SpecialtyId });

        modelBuilder.Entity<HospitalSpecialty>()
            .HasOne(hs => hs.Hospital)
            .WithMany(h => h.HospitalSpecialties)
            .HasForeignKey(hs => hs.HospitalId);

        modelBuilder.Entity<HospitalSpecialty>()
            .HasOne(hs => hs.Specialty)
            .WithMany(s => s.HospitalSpecialties)
            .HasForeignKey(hs => hs.SpecialtyId);

        // ─── Seed Data: Especialidades ────────────────────────────────────────
        // IDs fijos para poder referenciarlos desde la tabla pivote.
        // Estos datos se insertan automáticamente al correr la migration.
        modelBuilder.Entity<Specialty>().HasData(

            // Básicas — primer contacto con el sistema de salud
            new Specialty { Id = 1,  Name = "Medicina General",          Category = SpecialtyCategory.Basic },
            new Specialty { Id = 2,  Name = "Urgencias",                  Category = SpecialtyCategory.Basic },
            new Specialty { Id = 3,  Name = "Pediatría",                  Category = SpecialtyCategory.Basic },
            new Specialty { Id = 4,  Name = "Ginecología y Obstetricia",  Category = SpecialtyCategory.Basic },
            new Specialty { Id = 5,  Name = "Medicina Interna",           Category = SpecialtyCategory.Basic },
            new Specialty { Id = 6,  Name = "Medicina Familiar",          Category = SpecialtyCategory.Basic },

            // Quirúrgicas — requieren sala de operaciones
            new Specialty { Id = 7,  Name = "Cirugía General",            Category = SpecialtyCategory.Surgical },
            new Specialty { Id = 8,  Name = "Traumatología y Ortopedia",  Category = SpecialtyCategory.Surgical },
            new Specialty { Id = 9,  Name = "Neurocirugía",               Category = SpecialtyCategory.Surgical },
            new Specialty { Id = 10, Name = "Cirugía Plástica",           Category = SpecialtyCategory.Surgical },
            new Specialty { Id = 11, Name = "Cirugía Cardiovascular",     Category = SpecialtyCategory.Surgical },
            new Specialty { Id = 12, Name = "Urología",                   Category = SpecialtyCategory.Surgical },

            // Diagnóstico — apoyo clínico e interpretación de estudios
            new Specialty { Id = 13, Name = "Radiología e Imagen",        Category = SpecialtyCategory.Diagnostic },
            new Specialty { Id = 14, Name = "Patología",                  Category = SpecialtyCategory.Diagnostic },
            new Specialty { Id = 15, Name = "Laboratorio Clínico",        Category = SpecialtyCategory.Diagnostic },
            new Specialty { Id = 16, Name = "Medicina Nuclear",           Category = SpecialtyCategory.Diagnostic },
            new Specialty { Id = 17, Name = "Anestesiología",             Category = SpecialtyCategory.Diagnostic },

            // Subespecialidades — atención especializada por sistema u órgano
            new Specialty { Id = 18, Name = "Cardiología",                Category = SpecialtyCategory.Subspecialty },
            new Specialty { Id = 19, Name = "Neurología",                 Category = SpecialtyCategory.Subspecialty },
            new Specialty { Id = 20, Name = "Oncología",                  Category = SpecialtyCategory.Subspecialty },
            new Specialty { Id = 21, Name = "Dermatología",               Category = SpecialtyCategory.Subspecialty },
            new Specialty { Id = 22, Name = "Oftalmología",               Category = SpecialtyCategory.Subspecialty },
            new Specialty { Id = 23, Name = "Psiquiatría",                Category = SpecialtyCategory.Subspecialty },
            new Specialty { Id = 24, Name = "Endocrinología",             Category = SpecialtyCategory.Subspecialty },
            new Specialty { Id = 25, Name = "Gastroenterología",          Category = SpecialtyCategory.Subspecialty },
            new Specialty { Id = 26, Name = "Neumología",                 Category = SpecialtyCategory.Subspecialty },
            new Specialty { Id = 27, Name = "Nefrología",                 Category = SpecialtyCategory.Subspecialty },
            new Specialty { Id = 28, Name = "Hematología",                Category = SpecialtyCategory.Subspecialty },
            new Specialty { Id = 29, Name = "Infectología",               Category = SpecialtyCategory.Subspecialty },
            new Specialty { Id = 30, Name = "Rehabilitación",             Category = SpecialtyCategory.Subspecialty }
        );
    }
}