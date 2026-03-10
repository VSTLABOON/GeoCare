namespace GeoCare.Core.Entities;

public class Specialty
{
    public int             Id       { get; set; }
    public string          Name     { get; set; } = string.Empty; // "Cardiología", "Oncología"…
    // Categoría que agrupa las especialidades para los filtros del frontend:
    // Basic | Surgical | Diagnostic | Subspecialty
    public SpecialtyCategory Category { get; set; }

    // ─── Navegación ──────────────────────────────────────────────────────────
    // Una especialidad puede estar disponible en muchos hospitales.
    // La colección apunta a la tabla pivote HospitalSpecialty.
    public ICollection<HospitalSpecialty> HospitalSpecialties { get; set; } = [];
}