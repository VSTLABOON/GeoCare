using NetTopologySuite.Geometries;

namespace GeoCare.Core.Entities;

/// <summary>
/// Representa un establecimiento hospitalario importado del DENUE (INEGI).
/// La columna <see cref="Location"/> se almacena como <c>geometry</c> (SRID 4326)
/// en SQL Server mediante NetTopologySuite. Para obtener distancias en metros
/// directamente desde SQL Server, migrar a <c>geography</c> configurando el
/// tipo de columna con <c>.HasColumnType("geography")</c> en <see cref="AppDbContext"/>.
/// </summary>
public class Hospital
{
    public int    Id      { get; set; }
    public string Name    { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;

    // ─── Columna espacial ─────────────────────────────────────────────────────
    // Almacenada como 'geometry' (SRID 4326) por defecto con NetTopologySuite.
    // IMPORTANTE: Coordinate(X = longitud, Y = latitud) — orden obligatorio.
    // STDistance sobre 'geometry' devuelve grados, no metros. Por eso el
    // controller multiplica por MetrosPorGrado = 111 320 m/° (aprox. válido
    // en latitudes medias; introduce ~6 % de error en dirección E-O a 19 °N).
    // Migrar a 'geography' elimina esa conversión manual.
    public Point Location { get; set; } = Point.Empty;

    // ─── Tipo de institución ─────────────────────────────────────────────────
    // Inferido automáticamente desde la razón social del DENUE al importar.
    // Ver <see cref="InegiService.InferirTipo"/>.
    public HospitalType Type { get; set; } = HospitalType.Private;

    // ─── Estrato (Campo 6 del DENUE) ─────────────────────────────────────────
    // Personal ocupado según el DENUE. Solo se importan registros con
    // Estrato >= 4 (31–50 personas). Ver InegiService.EstratoMinimo.
    //   1 → 0–5     2 → 6–10    3 → 11–30   4 → 31–50
    //   5 → 51–100  6 → 101–250  7 → 251+
    public int Estrato { get; set; }

    // ─── Relaciones ───────────────────────────────────────────────────────────
    public ICollection<HospitalSpecialty> HospitalSpecialties { get; set; } = [];
    public ICollection<MedicalResource>   Resources           { get; set; } = [];

    // ─── Auditoría ────────────────────────────────────────────────────────────
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public string?  UpdatedBy   { get; set; }
}

/// <summary>
/// Clasifica los hospitales por institución operadora.
/// El orden de evaluación en <see cref="InegiService.InferirTipo"/> es relevante:
/// <see cref="ImssB"/> debe evaluarse ANTES que <see cref="Imss"/> porque
/// la razón social de IMSS Bienestar contiene la cadena "IMSS".
/// </summary>
public enum HospitalType
{
    /// <summary>Clínica u hospital de gestión privada (sin indicador público).</summary>
    Private = 0,

    /// <summary>Instituto Mexicano del Seguro Social.</summary>
    Imss = 1,

    /// <summary>IMSS-Bienestar (antes IMSS-Prospera / Solidaridad). Evaluar ANTES de Imss.</summary>
    ImssB = 2,

    /// <summary>Instituto de Seguridad y Servicios Sociales de los Trabajadores del Estado.</summary>
    Issste = 3,

    /// <summary>Secretaría de Salud federal / SESA estatal / Centros de Salud SS.</summary>
    SectorSalud = 4,

    /// <summary>Petróleos Mexicanos (hospital para trabajadores petroleros).</summary>
    Pemex = 5,

    /// <summary>Secretaría de la Defensa Nacional (hospitales militares).</summary>
    Sedena = 6,

    /// <summary>Secretaría de Marina (hospitales navales).</summary>
    Semar = 7,

    /// <summary>Cruz Roja Mexicana.</summary>
    CruzRoja = 8,

    /// <summary>Sistema Nacional para el Desarrollo Integral de la Familia.</summary>
    Dif = 9
}