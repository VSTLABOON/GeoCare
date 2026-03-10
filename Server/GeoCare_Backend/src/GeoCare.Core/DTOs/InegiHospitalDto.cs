using System.Text.Json.Serialization;

namespace GeoCare.Core.DTOs;

/// <summary>
/// Mapea la respuesta JSON del método <c>BuscarAreaActEstr</c> del DENUE.
///
/// ADVERTENCIA DE DESERIALIZACIÓN — case-sensitive matching:
/// <see cref="System.Text.Json.JsonSerializerOptions.PropertyNameCaseInsensitive"/> = true
/// aplica SOLO a propiedades SIN <c>[JsonPropertyName]</c>. Las que tienen el atributo
/// usan coincidencia EXACTA. Si el DENUE cambia la capitalización de un campo, el valor
/// quedará en <c>string.Empty</c> silenciosamente sin lanzar excepción.
///
/// Para diagnosticar un mismatch: revisar los logs de InegiService buscando
/// "sin id" &gt; 0 o campos vacíos en registros procesados.
///
/// Campos según documentación oficial de BuscarAreaActEstr:
///   Campo  2 → Id de establecimiento      ← deduplicación entre combinaciones SCIAN/estrato
///   Campo  3 → Nombre del establecimiento
///   Campo  4 → Razón social               ← inferencia del tipo de institución
///   Campo  5 → Clase de la actividad      ← nombre legible, no el código SCIAN
///   Campo  6 → Estrato (Personal ocupado) ← valores "1"–"7" como string
///   Campo  7 → Tipo de vialidad
///   Campo  8 → Calle
///   Campo  9 → Número exterior
///   Campo 18 → Longitud
///   Campo 19 → Latitud
/// </summary>
public class InegiHospitalDto
{
    // ── Campo 2 — Id de establecimiento ──────────────────────────────────────
    // Clave única en el DENUE. Usada para deduplicar entre combinaciones SCIAN × estrato.
    // El DENUE devuelve "Id" (I mayúscula, d minúscula) — matching case-sensitive.
    [JsonPropertyName("Id")]
    public string Id { get; set; } = string.Empty;

    // ── Campo 3 — Nombre ──────────────────────────────────────────────────────
    [JsonPropertyName("Nombre")]
    public string Nombre { get; set; } = string.Empty;

    // ── Campo 4 — Razón social ────────────────────────────────────────────────
    // Campo más confiable para inferir tipo de institución (IMSS, ISSSTE, etc.).
    // ATENCIÓN: el DENUE devuelve "Razon_social" con 's' minúscula.
    // Cambiar a "Razon_Social" rompe la deserialización silenciosamente.
    [JsonPropertyName("Razon_social")]
    public string RazonSocial { get; set; } = string.Empty;

    // ── Campo 5 — Clase de actividad ──────────────────────────────────────────
    // Nombre legible, ej: "Hospitales generales del sector público".
    // Se usa como tercer campo en InferirTipo y para auditoría en logs.
    [JsonPropertyName("Clase_actividad")]
    public string ClaseActividad { get; set; } = string.Empty;

    // ── Campo 6 — Estrato ─────────────────────────────────────────────────────
    // Personal ocupado como string numérico ("1"–"7").
    // Con BuscarAreaActEstr el DENUE ya garantiza el estrato por URL —
    // se parsea aquí solo para persistirlo en la entidad Hospital.
    [JsonPropertyName("Estrato")]
    public string Estrato { get; set; } = string.Empty;

    // ── Campos de dirección (Campos 7, 8, 9) ─────────────────────────────────
    [JsonPropertyName("Tipo_vialidad")]
    public string TipoVialidad { get; set; } = string.Empty;

    [JsonPropertyName("Calle")]
    public string Calle { get; set; } = string.Empty;

    [JsonPropertyName("Num_Exterior")]
    public string NumExterior { get; set; } = string.Empty;

    // ── Coordenadas (Campos 18, 19) ───────────────────────────────────────────
    // Devueltas como strings decimales con punto, ej: "-98.213456".
    // Se parsean con InvariantCulture en InegiService.
    [JsonPropertyName("Longitud")]
    public string Longitud { get; set; } = string.Empty;

    [JsonPropertyName("Latitud")]
    public string Latitud { get; set; } = string.Empty;
}