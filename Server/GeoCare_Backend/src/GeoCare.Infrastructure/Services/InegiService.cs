using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using GeoCare.Core.DTOs;
using GeoCare.Core.Entities;
using GeoCare.Core.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;

namespace GeoCare.Infrastructure.Services;

/// <summary>
/// Servicio de integración con el DENUE (Directorio Estadístico Nacional de Unidades Económicas)
/// del INEGI. Importa hospitales del estado de Puebla usando <c>BuscarAreaActEstr</c>,
/// que filtra por estrato de personal directamente en la URL (estratos 4–7, ≥ 31 personas).
///
/// Estrategia de búsqueda en cascada — el DENUE de Puebla clasifica establecimientos
/// de forma inconsistente entre niveles SCIAN:
///   · Nivel rama (4 dígitos): donde vive la mayoría de hospitales en Puebla.
///   · Nivel clase (6 dígitos): máxima precisión, menos registros en la práctica.
///   · Por nombre: captura establecimientos mal clasificados en SCIAN pero con
///     "hospital" en la razón social.
/// La deduplicación por Id garantiza que un registro que aparezca en múltiples
/// combinaciones solo se importe una vez.
/// </summary>
public class InegiService
{
    private readonly HttpClient            _httpClient;
    private readonly string               _token;
    private readonly GeometryFactory      _geometryFactory;
    private readonly ILogger<InegiService> _logger;

    // Instancia estática para evitar allocations repetidas en cada página.
    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    // ── Constantes de configuración ──────────────────────────────────────────

    /// <summary>Clave INEGI del estado de Puebla (01–32).</summary>
    private const string EntidadPuebla = "21";

    /// <summary>
    /// Registros por página. 500 equilibra velocidad vs estabilidad del stream.
    /// </summary>
    private const int TamanoPagina = 500;

    /// <summary>Pausa base entre páginas (ms). Se dobla en cada reintento (backoff exponencial).</summary>
    private const int DelayBaseMs = 1_000;

    /// <summary>Intentos máximos ante errores transitorios (502/503/504, stream cortado, timeout).</summary>
    private const int MaxReintentos = 5;

    // ── Jerarquía SCIAN ──────────────────────────────────────────────────────

    /// <summary>
    /// Determina en qué posición de la URL de BuscarAreaActEstr se coloca el código.
    /// BuscarAreaActEstr/{entidad}/…/{sector}/{subsector}/{rama}/{clase}/{nombre}/…
    /// </summary>
    private enum NivelScian
    {
        /// <summary>Código de 4 dígitos → posición {rama} en la URL.</summary>
        Rama,
        /// <summary>Código de 6 dígitos → posición {clase} en la URL.</summary>
        Clase,
        /// <summary>Término de texto → posición {nombre} en la URL.</summary>
        Nombre
    }

    private sealed record CodigoScian(string Valor, NivelScian Nivel);

    /// <summary>
    /// Códigos a consultar en cascada. El orden es irrelevante para el resultado
    /// (la deduplicación por Id elimina duplicados), pero sí afecta el orden de
    /// importación en los logs.
    ///
    /// ── Nivel rama (4 dígitos) ──
    ///   6211  → Hospitales (todos los subtipos: 621111–621119)
    ///   6212  → Clínicas con internamiento (todos los subtipos)
    ///
    /// ── Nivel clase (6 dígitos) ──
    ///   621111 → Hospital general público
    ///   621112 → Hospital general privado
    ///   621113 → Hospital de especialidad público
    ///   621114 → Hospital de especialidad privado
    ///   621210 → Clínicas y similares con internamiento
    ///
    /// ── Por nombre ──
    ///   "hospital" → establecimientos con esa palabra en razón social o nombre
    /// </summary>
    private static readonly CodigoScian[] CodigosHospital =
    [
        // Rama primero — captura el mayor volumen con menos peticiones
        new("6211",      NivelScian.Rama),
        new("6212",      NivelScian.Rama),

        // Clase — complementa registros clasificados con máxima precisión
        new("621111",    NivelScian.Clase),
        new("621112",    NivelScian.Clase),
        new("621113",    NivelScian.Clase),
        new("621114",    NivelScian.Clase),
        new("621210",    NivelScian.Clase),

        // Por nombre — fallback para registros mal clasificados en SCIAN
        new("hospital",  NivelScian.Nombre),
    ];

    /// <summary>
    /// Estratos solicitados al DENUE — 31 personas en adelante.
    /// BuscarAreaActEstr acepta un estrato por petición.
    ///   4 → 31–50  · 5 → 51–100  · 6 → 101–250  · 7 → 251+
    /// Total de combinaciones: 8 códigos × 4 estratos = 32 peticiones base.
    /// </summary>
    private static readonly int[] EstratosFiltro = [4, 5, 6, 7];

    // ────────────────────────────────────────────────────────────────────────

    public InegiService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<InegiService> logger)
    {
        _httpClient = httpClient;
        _logger     = logger;

        var rawToken = configuration["INEGI:Token"];
        _token = rawToken?.Trim()
                 ?? throw new InvalidOperationException(
                     "Token INEGI no encontrado. Configúralo con:\n" +
                     "  dotnet user-secrets set \"INEGI:Token\" \"tu-token\" --project GeoCare.Api");

        _geometryFactory = new GeometryFactory(new PrecisionModel(), 4326);
    }

    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Descarga todos los hospitales del estado de Puebla desde el DENUE,
    /// usando una estrategia en cascada por nivel SCIAN × estrato con paginación
    /// completa. El filtro de estrato (≥ 4, es decir ≥ 31 personas) lo aplica el
    /// servidor INEGI, no el cliente.
    /// </summary>
    /// <returns>Lista de entidades <see cref="Hospital"/> listas para persistir.</returns>
    /// <exception cref="HttpRequestException">
    /// Si alguna combinación falla tras <see cref="MaxReintentos"/> intentos.
    /// </exception>
    /// <exception cref="InvalidDataException">
    /// Si el DENUE devuelve HTML (token incorrecto o expirado).
    /// </exception>
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<List<Hospital>> ImportarTodosLosHospitalesDePuebla()
    {
        var resultado = new List<Hospital>();
        var idsVistos = new HashSet<string>(); // deduplicación global entre todas las combinaciones

        _logger.LogInformation(
            "Iniciando importación — BuscarAreaActEstr | {N} códigos | estratos: {Estratos}",
            CodigosHospital.Length,
            string.Join(',', EstratosFiltro));

        foreach (var codigo in CodigosHospital)
        {
            foreach (var estrato in EstratosFiltro)
            {
                _logger.LogInformation(
                    "┌ {Nivel} [{Valor}] | Estrato {Estrato} ({Desc})",
                    codigo.Nivel, codigo.Valor, estrato, EstratoHelper.Describir(estrato));

                int regInicial = 1;
                int pagina     = 1;

                while (true)
                {
                    int regFinal = regInicial + TamanoPagina - 1;
                    var ruta     = BuildRuta(codigo, regInicial, regFinal, estrato);

                    _logger.LogInformation(
                        "│  Página {Pagina} → reg {Ini}–{Fin}",
                        pagina, regInicial, regFinal);

                    var (rawContent, finDatos) =
                        await FetchConRetryAsync(ruta, codigo.Valor, estrato, pagina);

                    if (finDatos) break;

                    // El DENUE devuelve texto plano cuando no hay más datos,
                    // no un array vacío. Deserializarlo como List<T> lanzaría
                    // JsonException y abortaría toda la importación.
                    if (!rawContent.TrimStart().StartsWith('['))
                    {
                        _logger.LogInformation(
                            "│  Fin de datos para [{Valor}]/estrato {Estrato} (respuesta no-JSON).",
                            codigo.Valor, estrato);
                        break;
                    }

                    List<InegiHospitalDto>? lote;
                    try
                    {
                        lote = JsonSerializer.Deserialize<List<InegiHospitalDto>>(rawContent, JsonOpts);
                    }
                    catch (JsonException jex)
                    {
                        _logger.LogError(jex,
                            "│  Error de deserialización en [{Valor}]/estrato {Estrato}, página {Pagina}.",
                            codigo.Valor, estrato, pagina);
                        _logger.LogDebug("│  Body: {Body}",
                            rawContent[..Math.Min(300, rawContent.Length)]);
                        break;
                    }

                    if (lote is null || lote.Count == 0)
                    {
                        _logger.LogInformation(
                            "│  Array vacío en [{Valor}]/estrato {Estrato}.",
                            codigo.Valor, estrato);
                        break;
                    }

                    var (cNuevos, cDupes, cSinCoord, cSinId) =
                        ProcesarLote(lote, idsVistos, resultado);

                    _logger.LogInformation(
                        "│  Pág {Pagina}: {Recibidos} recibidos → {Nuevos} nuevos | {Dupes} dupes | {SinCoord} sin coords | {SinId} sin id",
                        pagina, lote.Count, cNuevos, cDupes, cSinCoord, cSinId);

                    if (lote.Count < TamanoPagina)
                    {
                        _logger.LogInformation(
                            "└  Última página de [{Valor}]/estrato {Estrato}.",
                            codigo.Valor, estrato);
                        break;
                    }

                    regInicial += TamanoPagina;
                    pagina++;
                    await Task.Delay(DelayBaseMs);
                }
            }
        }

        _logger.LogInformation(
            "Importación completa: {Total} hospitales (estratos {Estratos}).",
            resultado.Count, string.Join(',', EstratosFiltro));

        return resultado;
    }

    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Construye la URL de <c>BuscarAreaActEstr</c> colocando el código SCIAN
    /// en la posición correcta según su nivel jerárquico.
    ///
    /// Formato completo de la URL:
    ///   BuscarAreaActEstr/{entidad}/{mpio}/{loc}/{ageb}/{manzana}/
    ///                    {sector}/{subsector}/{rama}/{clase}/{nombre}/
    ///                    {regIni}/{regFin}/{id}/{estrato}/{token}
    ///
    /// Mapeo por nivel:
    ///   Rama   (4 díg) → rama={valor}, clase=0, nombre=0
    ///   Clase  (6 díg) → rama=0,       clase={valor}, nombre=0
    ///   Nombre (texto) → rama=0,        clase=0, nombre={valor}
    /// </summary>
    private string BuildRuta(CodigoScian codigo, int regInicial, int regFinal, int estrato)
    {
        var (rama, clase, nombre) = codigo.Nivel switch
        {
            NivelScian.Rama   => (codigo.Valor, "0",           "0"),
            NivelScian.Clase  => ("0",          codigo.Valor,  "0"),
            NivelScian.Nombre => ("0",          "0",           codigo.Valor),
            _                 => throw new ArgumentOutOfRangeException(nameof(codigo))
        };

        return $"BuscarAreaActEstr/{EntidadPuebla}/0/0/0/0/0/0/{rama}/{clase}/{nombre}/" +
               $"{regInicial}/{regFinal}/0/{estrato}/{_token}";
    }

    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Realiza una petición GET al DENUE con reintentos ante errores transitorios
    /// usando backoff exponencial. Devuelve el contenido raw y una bandera
    /// <c>FinDatos</c> que indica si el servidor señaló fin de datos con HTTP 0.
    /// </summary>
    // ─────────────────────────────────────────────────────────────────────────
    private async Task<(string RawContent, bool FinDatos)> FetchConRetryAsync(
        string ruta,
        string valorCodigo,
        int    estrato,
        int    pagina)
    {
        for (int intento = 1; intento <= MaxReintentos; intento++)
        {
            try
            {
                using var response = await _httpClient.GetAsync(
                    ruta, HttpCompletionOption.ResponseHeadersRead);

                // HTTP 0: el DENUE señala "sin registros" con status inválido.
                // No leer el body — el servidor cierra la conexión inmediatamente
                // tras los headers y ReadAsStringAsync() fallaría.
                if ((int)response.StatusCode == 0)
                {
                    var contentLength = response.Content.Headers.ContentLength ?? 0;
                    _logger.LogWarning(
                        "│  HTTP 0 (Content-Length={ContentLength}) — fin de datos [{Valor}]/estrato {Estrato}.",
                        contentLength, valorCodigo, estrato);
                    return (string.Empty, FinDatos: true);
                }

                if (response.StatusCode is HttpStatusCode.BadGateway        // 502
                                       or HttpStatusCode.ServiceUnavailable  // 503
                                       or HttpStatusCode.GatewayTimeout)     // 504
                {
                    throw new IOException(
                        $"DENUE respondió {(int)response.StatusCode} (transitorio).");
                }

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException(
                        $"El DENUE respondió {(int)response.StatusCode}. " +
                        $"Cuerpo: {body[..Math.Min(300, body.Length)]}",
                        null,
                        response.StatusCode);
                }

                var rawContent = await response.Content.ReadAsStringAsync();

                if (rawContent.TrimStart().StartsWith('<'))
                    throw new InvalidDataException(
                        "El DENUE devolvió HTML. Verifica el token con: " +
                        "dotnet user-secrets list --project GeoCare.Api");

                return (rawContent, FinDatos: false);
            }
            catch (Exception ex) when (intento < MaxReintentos && EsTransitorio(ex))
            {
                int espera = DelayBaseMs * (int)Math.Pow(2, intento - 1);
                _logger.LogWarning(ex,
                    "│  Error transitorio (intento {Intento}/{Max}). Reintentando en {Segundos}s...",
                    intento, MaxReintentos, espera / 1000.0);
                await Task.Delay(espera);
            }
        }

        throw new HttpRequestException(
            $"[INEGI] [{valorCodigo}]/estrato {estrato}, página {pagina}: " +
            $"falló tras {MaxReintentos} intentos.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Procesa un lote de registros del DENUE añadiendo los válidos al acumulador.
    /// El estrato ya está garantizado por la URL — no se re-valida aquí.
    /// Devuelve contadores para el log de progreso.
    /// </summary>
    // ─────────────────────────────────────────────────────────────────────────
    private (int Nuevos, int Dupes, int SinCoord, int SinId) ProcesarLote(
        List<InegiHospitalDto> lote,
        HashSet<string>        idsVistos,
        List<Hospital>         acumulador)
    {
        int cNuevos   = 0;
        int cDupes    = 0;
        int cSinCoord = 0;
        int cSinId    = 0;

        foreach (var item in lote)
        {
            // Id vacío indica mismatch de [JsonPropertyName] (case-sensitive).
            // Se loguea solo la primera ocurrencia para no saturar los logs.
            if (string.IsNullOrWhiteSpace(item.Id))
            {
                cSinId++;
                if (cSinId == 1)
                    _logger.LogWarning(
                        "│  ADVERTENCIA: item.Id vacío en '{Nombre}'. " +
                        "Posible mismatch en [JsonPropertyName(\"Id\")] de InegiHospitalDto.",
                        item.Nombre);

                // No deduplicar con Id vacío: todos colisionarían en el HashSet
                // con la primera entrada de string vacío. Se agrega sin deduplicar.
            }
            else if (!idsVistos.Add(item.Id))
            {
                cDupes++;
                continue;
            }

            if (!double.TryParse(item.Latitud,  CultureInfo.InvariantCulture, out var lat) ||
                !double.TryParse(item.Longitud, CultureInfo.InvariantCulture, out var lng))
            {
                cSinCoord++;
                continue;
            }

            // TryParse solo para persistir el valor — el estrato ya viene garantizado por la URL.
            _ = int.TryParse(item.Estrato, out var estrato);

            acumulador.Add(new Hospital
            {
                Name        = item.Nombre.Trim(),
                Address     = BuildAddress(item),
                Location    = _geometryFactory.CreatePoint(new Coordinate(lng, lat)),
                Type        = InferirTipo(item.RazonSocial, item.Nombre, item.ClaseActividad),
                Estrato     = estrato,
                LastUpdated = DateTime.UtcNow,
                UpdatedBy   = "INEGI_IMPORT"
            });

            cNuevos++;
        }

        return (cNuevos, cDupes, cSinCoord, cSinId);
    }

    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Ensambla la dirección desde los campos del DENUE omitiendo segmentos vacíos
    /// para evitar espacios dobles cuando <c>NumExterior</c> o <c>TipoVialidad</c>
    /// están vacíos en el registro.
    /// </summary>
    // ─────────────────────────────────────────────────────────────────────────
    private static string BuildAddress(InegiHospitalDto item)
    {
        var partes = new[] { item.TipoVialidad, item.Calle, item.NumExterior }
            .Where(p => !string.IsNullOrWhiteSpace(p));
        return string.Join(' ', partes);
    }

    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Infiere el <see cref="HospitalType"/> combinando razón social, nombre del
    /// establecimiento y clase de actividad económica del DENUE.
    ///
    /// ORDEN DE EVALUACIÓN — importa por solapamiento de términos:
    ///   1. ImssB      — ANTES que Imss: razón social de IMSS Bienestar contiene "IMSS".
    ///   2. Imss
    ///   3. Issste     — incluye variante "ISSTE" (sin segunda S).
    ///   4. Pemex
    ///   5. Sedena     — "MILITAR" solo en razón social/nombre.
    ///   6. Semar      — "ARMADA" solo en razón social.
    ///   7. CruzRoja
    ///   8. Dif        — solo en razón social con delimitadores.
    ///   9. SectorSalud — solo en razón social con términos específicos.
    ///  10. Private    — fallback.
    ///
    /// Todos los campos se normalizan (sin acentos, mayúsculas) antes de comparar.
    /// </summary>
    // ─────────────────────────────────────────────────────────────────────────
    private static HospitalType InferirTipo(
        string razonSocial,
        string nombre,
        string claseActividad)
    {
        var rs     = NormalizarTexto(razonSocial);
        var nom    = NormalizarTexto(nombre);
        var act    = NormalizarTexto(claseActividad);
        var fuente = $"{rs} {nom} {act}";

        // 1. IMSS-Bienestar — evaluar ANTES de IMSS
        if (fuente.Contains("BIENESTAR"))
            return HospitalType.ImssB;

        // 2. IMSS
        if (fuente.Contains("IMSS"))
            return HospitalType.Imss;

        // 3. ISSSTE (variante "ISSTE" en registros históricos del DENUE)
        if (fuente.Contains("ISSSTE") || fuente.Contains("ISSTE"))
            return HospitalType.Issste;

        // 4. PEMEX
        if (fuente.Contains("PEMEX") || fuente.Contains("PETROLEOS MEXICANOS"))
            return HospitalType.Pemex;

        // 5. SEDENA — "MILITAR" solo en razón social/nombre
        if (rs.Contains("SEDENA")           || nom.Contains("SEDENA")           ||
            rs.Contains("DEFENSA NACIONAL") || nom.Contains("DEFENSA NACIONAL") ||
            rs.Contains("HOSPITAL MILITAR") || nom.Contains("HOSPITAL MILITAR"))
            return HospitalType.Sedena;

        // 6. SEMAR — "ARMADA" solo en razón social para evitar falso positivo
        //    con colonias llamadas "La Marina" o similares
        if (fuente.Contains("SEMAR") || rs.Contains("ARMADA DE MEXICO"))
            return HospitalType.Semar;

        // 7. Cruz Roja
        if (fuente.Contains("CRUZ ROJA"))
            return HospitalType.CruzRoja;

        // 8. DIF — solo en razón social con delimitadores
        //    para evitar "dif" como subcadena de otras palabras
        if (rs == "DIF"           ||
            rs.StartsWith("DIF ") ||
            rs.EndsWith(" DIF")   ||
            rs.Contains(" DIF "))
            return HospitalType.Dif;

        // 9. Sector Salud — solo en razón social con términos específicos.
        //    "SALUD" suelto clasificaría mal hospitales privados.
        if (rs.Contains("SECRETARIA DE SALUD")      ||
            rs.Contains("SECRETARIA DE SALUBRIDAD") ||
            rs.Contains("SERVICIOS DE SALUD")       ||
            rs.Contains("JURISDICCION SANITARIA")   ||
            rs.Contains("SESA")                     ||
            rs.Contains("SSA")                      ||
            rs.Contains("CESSA")                    ||
            rs.Contains("COPLAMAR"))
            return HospitalType.SectorSalud;

        // 10. Privado (fallback)
        return HospitalType.Private;
    }

    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Convierte el texto a mayúsculas y elimina diacríticos (acentos, ü, ñ → N)
    /// para comparaciones robustas ante inconsistencias tipográficas del DENUE.
    /// Ejemplo: "Secretaría de Salud" → "SECRETARIA DE SALUD".
    /// </summary>
    // ─────────────────────────────────────────────────────────────────────────
    private static string NormalizarTexto(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;

        var normalizado = input.Normalize(NormalizationForm.FormD);
        var sb          = new StringBuilder(normalizado.Length);

        foreach (var c in normalizado)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        return sb.ToString().ToUpperInvariant();
    }

    /// <summary>
    /// Determina si una excepción es transitoria y justifica un reintento.
    /// Los errores HTTP reales (4xx/5xx con StatusCode concreto) fallan rápido
    /// sin reintentos innecesarios.
    /// </summary>
    private static bool EsTransitorio(Exception ex) => ex switch
    {
        IOException                               => true,  // stream cortado
        TaskCanceledException                     => true,  // timeout del HttpClient
        HttpRequestException { StatusCode: null } => true,  // error de red envuelto
        HttpRequestException hre when
            hre.StatusCode is HttpStatusCode.BadGateway
                           or HttpStatusCode.ServiceUnavailable
                           or HttpStatusCode.GatewayTimeout    => true,
        _                                                       => false
    };

    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Consulta el endpoint <c>Buscar</c> del DENUE para encontrar hospitales
    /// en un radio máximo de 5 000 metros alrededor de un punto geográfico.
    /// No aplica filtro de estrato — útil solo para pruebas desde Swagger.
    /// Para importación masiva usar <see cref="ImportarTodosLosHospitalesDePuebla"/>.
    /// </summary>
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<List<Hospital>> BuscarHospitalesCerca(double lat, double lng, int metros)
    {
        if (metros is < 1 or > 5000)
            throw new ArgumentOutOfRangeException(nameof(metros),
                "El DENUE acepta un radio de 1 a 5 000 metros.");

        var latStr = lat.ToString(CultureInfo.InvariantCulture);
        var lngStr = lng.ToString(CultureInfo.InvariantCulture);
        var ruta   = $"Buscar/hospital/{latStr},{lngStr}/{metros}/{_token}";

        _logger.LogInformation("BuscarHospitalesCerca → {Base}{Ruta}",
            _httpClient.BaseAddress, ruta);

        using var response = await _httpClient.GetAsync(ruta);
        var rawContent     = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"El DENUE respondió {(int)response.StatusCode}. " +
                $"Cuerpo: {rawContent[..Math.Min(300, rawContent.Length)]}");

        if (rawContent.TrimStart().StartsWith('<'))
            throw new InvalidDataException(
                $"El DENUE devolvió HTML. Cuerpo: {rawContent[..Math.Min(300, rawContent.Length)]}");

        if (!rawContent.TrimStart().StartsWith('['))
            return [];

        var inegiData = JsonSerializer.Deserialize<List<InegiHospitalDto>>(rawContent, JsonOpts);
        if (inegiData is null) return [];

        var lista = new List<Hospital>(inegiData.Count);

        foreach (var item in inegiData)
        {
            if (!double.TryParse(item.Latitud,  CultureInfo.InvariantCulture, out var latitude)  ||
                !double.TryParse(item.Longitud, CultureInfo.InvariantCulture, out var longitude))
                continue;

            _ = int.TryParse(item.Estrato, out var estrato);

            lista.Add(new Hospital
            {
                Name        = item.Nombre.Trim(),
                Address     = BuildAddress(item),
                Location    = _geometryFactory.CreatePoint(new Coordinate(longitude, latitude)),
                Type        = InferirTipo(item.RazonSocial, item.Nombre, item.ClaseActividad),
                Estrato     = estrato,
                LastUpdated = DateTime.UtcNow,
                UpdatedBy   = "INEGI_IMPORT"
            });
        }

        return lista;
    }
}