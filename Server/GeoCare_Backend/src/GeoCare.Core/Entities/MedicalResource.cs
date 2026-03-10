namespace GeoCare.Core.Entities;

// Inventario de recursos físicos de un hospital.
// Un hospital puede tener muchos recursos de distintos tipos.
// Los conteos se actualizan manualmente (el DENUE no provee esta información).
public class MedicalResource
{
    public int Id         { get; set; }
    public int HospitalId { get; set; }

    // Categoría del recurso — permite filtrar por tipo en el frontend
    public ResourceType Type { get; set; }
    // Nombre descriptivo del recurso: "Cama UCI", "Tomógrafo", "EKG"…
    public string Name { get; set; } = string.Empty;
    // Cantidad disponible en el hospital al momento de la última actualización
    public int Qty { get; set; }

    // ─── Auditoría ────────────────────────────────────────────────────────────
    public DateTime UpdatedAt  { get; set; } = DateTime.UtcNow;
    public string?  UpdatedBy  { get; set; }

    // ─── Navegación ───────────────────────────────────────────────────────────
    public Hospital Hospital { get; set; } = null!;
}

public enum ResourceType
{
    Bed,            // Cama general
    ICUBed,         // Cama de Terapia Intensiva
    ObstetricBed,   // Cama obstétrica / cunero
    OperatingRoom,  // Quirófano
    Ambulance,
    Equipment       // EKG, Rayos X, Tomógrafo, Ultrasonido…
}