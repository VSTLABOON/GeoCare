namespace GeoCare.Core.Entities;

// Tabla pivote explícita para la relación muchos-a-muchos entre Hospital y Specialty.
// Se hace explícita (en lugar de dejar que EF la genere automáticamente)
// para poder agregar propiedades propias de la relación, como el horario
// y si esa especialidad atiende urgencias en ese hospital específico.
public class HospitalSpecialty
{
    // ─── Clave compuesta (definida en AppDbContext) ───────────────────────────
    public int HospitalId  { get; set; }
    public int SpecialtyId { get; set; } // ← sin 'd' extra (era el typo original)

    // ─── Propiedades de la relación ───────────────────────────────────────────
    // Indica si esta especialidad tiene servicio de urgencias en este hospital.
    public bool    HandlesEmergencies { get; set; }
    // Horario de consulta externa para esta especialidad en este hospital.
    // Formato sugerido: "Lun-Vie 08:00-14:00"
    public string? Schedule           { get; set; }

    // ─── Navegación ───────────────────────────────────────────────────────────
    public Hospital  Hospital  { get; set; } = null!;
    public Specialty Specialty { get; set; } = null!;
}