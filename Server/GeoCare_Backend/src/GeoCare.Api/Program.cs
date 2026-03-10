using GeoCare.Infrastructure.Data;
using GeoCare.Infrastructure.Services;
using GeoCare.Core.Entities;
using GeoCare.Core.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http.Timeouts;

var builder = WebApplication.CreateBuilder(args);

// ─────────────────────────────────────────────────────────────────────────────
// 1. BASE DE DATOS con soporte espacial (NetTopologySuite)
// ─────────────────────────────────────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString, x => x.UseNetTopologySuite()));

// ─────────────────────────────────────────────────────────────────────────────
// 2. IDENTITY + JWT Bearer
// ─────────────────────────────────────────────────────────────────────────────
builder.Services.AddAuthorization();
builder.Services.AddAuthentication()
    .AddBearerToken(IdentityConstants.BearerScheme);

builder.Services.AddIdentityCore<ApplicationUser>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddApiEndpoints();

// ─────────────────────────────────────────────────────────────────────────────
// 3. SWAGGER + CONTROLLERS
// ─────────────────────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters
            .Add(new NetTopologySuite.IO.Converters.GeoJsonConverterFactory());
    });

// ─────────────────────────────────────────────────────────────────────────────
// 4. CONFIGURACIONES TIPADAS
// ─────────────────────────────────────────────────────────────────────────────
builder.Services.Configure<GoogleOptions>(
    builder.Configuration.GetSection(GoogleOptions.SectionName));

// ─────────────────────────────────────────────────────────────────────────────
// 5. HTTP CLIENT PARA EL DENUE
//
// · Timeout de 30s por petición individual al DENUE — suficiente para una
//   página de 500 registros. El proceso total de importación puede durar
//   varios minutos pero ese tiempo lo gestiona el retry/backoff en InegiService,
//   no este timeout (que aplica request a request, no al proceso completo).
// ─────────────────────────────────────────────────────────────────────────────
builder.Services.AddHttpClient<InegiService>(client =>
{
    client.BaseAddress = new Uri("https://www.inegi.org.mx/app/api/denue/v1/consulta/");
    client.Timeout     = TimeSpan.FromSeconds(30);
});

// ─────────────────────────────────────────────────────────────────────────────
// 6. REQUEST TIMEOUTS
//
// Política "import" sin límite de tiempo para el endpoint de importación DENUE,
// que puede tardar varios minutos procesando todas las clases SCIAN.
// Los demás endpoints usan el timeout por defecto de 30s.
//
// Uso en el controller de importación:
//   [RequestTimeout("import")]
//   public async Task<IActionResult> ImportarHospitales(...) { ... }
// ─────────────────────────────────────────────────────────────────────────────

builder.Services.AddSingleton<ImportJobService>();

builder.Services.AddRequestTimeouts(options =>
{
    options.DefaultPolicy = new RequestTimeoutPolicy
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    options.AddPolicy("import", new RequestTimeoutPolicy
    {
        // Sin timeout: la importación es un proceso largo controlado internamente
        // por InegiService (retries + backoff). Cortarlo desde aquí causaría
        // que la operación quede a medias en la base de datos.
        Timeout = Timeout.InfiniteTimeSpan
    });
});

// ─────────────────────────────────────────────────────────────────────────────
// 7. CORS
// Permite que el frontend (Vite en :5173) consuma la API durante desarrollo.
// En producción, cambiar la URL por el dominio real del frontend.
// ─────────────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevPolicy", policy =>
        policy.WithOrigins(
                "http://localhost:5173",   // Vite dev server
                "https://localhost:5173"
              )
              .AllowAnyMethod()
              .AllowAnyHeader());
});

// ─────────────────────────────────────────────────────────────────────────────
// 8. PIPELINE
// ─────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("DevPolicy");           // ← siempre primero

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseRequestTimeouts();           // ← antes de UseAuthorization
app.UseAuthorization();

app.MapIdentityApi<ApplicationUser>();
app.MapControllers();

app.Run();