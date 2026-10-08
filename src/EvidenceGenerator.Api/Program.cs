using System.Net;
using EvidenceGenerator.Api.Documents;
using EvidenceGenerator.Api.Excel;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = DocumentValidation.MaxRequestBytes);
builder.Services.AddProblemDetails();
builder.Services.AddSingleton(_ => new DocumentStore(builder.Configuration["DataDirectory"] ?? Path.Combine(builder.Environment.ContentRootPath, "App_Data")));
builder.Services.AddSingleton<WorkspaceStore>();
builder.Services.AddSingleton<IExcelGenerator>(new TemplateExcelGenerator(Path.Combine(builder.Environment.ContentRootPath, "Templates", "PruebasUnitarias.v1.xlsx")));
var app = builder.Build();
app.UseExceptionHandler();
app.Use(async (context, next) =>
{
    var origin = context.Request.Headers.Origin.ToString();
    var allowedOrigins = new[] { "http://localhost:5173", "http://127.0.0.1:5173", "http://localhost:5080", "http://127.0.0.1:5080" };
    if (context.Request.Host.Host is not ("localhost" or "127.0.0.1" or "[::1]") || (context.Connection.RemoteIpAddress is { } ip && !IPAddress.IsLoopback(ip)) || (origin.Length > 0 && !allowedOrigins.Contains(origin)))
    { context.Response.StatusCode = 403; return; }
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers.CacheControl = "no-store";
    await next();
});
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapGet("/api/health", () => Results.Ok(new { status = "ok", template = "PruebasUnitarias.v1" }));
app.MapGet("/api/storage", (DocumentStore store) => Results.Ok(new { databasePath = store.DatabasePath, provider = "SQLite" }));
app.MapGet("/api/settings", (WorkspaceStore store) => Results.Ok(store.Get()));
app.MapPut("/api/settings", (WorkspaceSettings settings, WorkspaceStore store) =>
{
    if (WorkspaceStore.Validate(settings) is { } error) return Results.BadRequest(new { detail = error });
    return store.Save(settings) is { } saved ? Results.Ok(saved) : Results.Conflict(new { detail = "La configuración cambió en otra ventana. Vuelve a abrir Configuración antes de guardar." });
});
app.MapGet("/api/exports/location", (string requirement, WorkspaceStore store) => Results.Ok(store.Location(requirement)));
app.MapPost("/api/exports/save", (SaveExcelRequest request, WorkspaceStore store, IExcelGenerator excel) =>
{
    if (request.Document is null) return Results.BadRequest(new { detail = "Falta el documento." });
    if (DocumentValidation.Validate(request.Document, true) is { } error) return Results.BadRequest(new { detail = error });
    var directory = request.Directory ?? store.Get().ExcelDirectory;
    if (!string.IsNullOrWhiteSpace(directory) && !WorkspaceStore.ValidDirectory(directory)) return Results.BadRequest(new { detail = "Indica una carpeta local absoluta válida." });
    try { return Results.Ok(store.WriteExcel(request.Document.Requirement, directory, excel.Generate(request.Document))); }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
    { return Results.BadRequest(new { detail = "No se pudo guardar el Excel. Cierra el archivo si está abierto y revisa la ruta, el espacio y los permisos de la carpeta. El archivo anterior se conserva si no fue posible reemplazarlo." }); }
});
app.MapGet("/api/documents", (DocumentStore store, string? search, DateOnly? from, DateOnly? to, int? offset) =>
    from > to || (search?.Length ?? 0) > 4000 || offset < 0
        ? Results.BadRequest(new { detail = "Revisa el texto de búsqueda y el rango de fechas." })
        : Results.Ok(store.List(search, from, to, offset ?? 0)));
app.MapGet("/api/documents/{id:guid}", (Guid id, DocumentStore store) => store.Get(id) is { } doc ? Results.Ok(doc) : Results.NotFound());
app.MapDelete("/api/documents/{id:guid}", (Guid id, int revision, DocumentStore store) =>
{
    if (revision < 1) return Results.BadRequest(new { detail = "Revisión inválida." });
    if (store.Delete(id, revision)) return Results.NoContent();
    return store.Get(id) is null
        ? Results.NotFound(new { detail = "El documento ya no existe. Actualiza la búsqueda." })
        : Results.Conflict(new { detail = "El documento cambió en otra ventana. Actualiza la búsqueda antes de eliminarlo." });
});
app.MapPut("/api/documents/{id:guid}", (Guid id, EvidenceDocument doc, DocumentStore store) =>
{
    if (id != doc.Id) return Results.BadRequest(new { detail = "El identificador no coincide." });
    if (DocumentValidation.Validate(doc) is { } error) return Results.BadRequest(new { detail = error });
    return store.Save(doc) is { } saved ? Results.Ok(saved) : Results.Conflict(new { detail = "El documento cambió en otra ventana. Abre la versión guardada antes de continuar." });
});
app.MapPost("/api/exports/excel", (EvidenceDocument doc, IExcelGenerator excel) =>
{
    if (DocumentValidation.Validate(doc, true) is { } error) return Results.BadRequest(new { detail = error });
    return Results.File(excel.Generate(doc), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Pruebas Unitarias - {doc.Requirement}.xlsx");
});
app.Map("/api/{**path}", () => Results.NotFound());
app.MapFallbackToFile("index.html");
app.Run();
public partial class Program;
