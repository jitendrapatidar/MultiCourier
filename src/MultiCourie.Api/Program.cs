using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using MultiCourie.Api.Middleware;
using MultiCourie.Application;
using MultiCourie.Infrastructure;
using MultiCourie.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
 // application can still start.
var swaggerRegistered = false;
try
{
    builder.Services.AddSwaggerGen();
    swaggerRegistered = true;
}
catch (TypeLoadException ex)
{
    // Surface a simple console message during startup; for deeper diagnosis
    
    Console.WriteLine($"Warning: Skipped AddSwaggerGen due to TypeLoadException: {ex.Message}");
    swaggerRegistered = false;
}

// Log currently loaded assemblies to help diagnose type load problems
Console.WriteLine("Assemblies loaded before Build():");
foreach (var a in AppDomain.CurrentDomain.GetAssemblies().OrderBy(x => x.FullName))
{
    Console.WriteLine(a.FullName);
}

WebApplication app;
try
{
    app = builder.Build();
}
catch (System.Reflection.ReflectionTypeLoadException rtle)
{
    Console.WriteLine("ReflectionTypeLoadException while building the app: " + rtle.Message);
    if (rtle.LoaderExceptions != null)
    {
        foreach (var lex in rtle.LoaderExceptions)
        {
            if (lex == null) continue;
            Console.WriteLine("-- LoaderException: " + lex.GetType().FullName + " : " + lex.Message);
            Console.WriteLine(lex.StackTrace);
            if (lex is System.IO.FileNotFoundException fnf && !string.IsNullOrEmpty(fnf.FusionLog))
            {
                Console.WriteLine("FusionLog: " + fnf.FusionLog);
            }
        }
    }
    Console.WriteLine("Assemblies loaded at failure:");
    foreach (var a in AppDomain.CurrentDomain.GetAssemblies().OrderBy(x => x.FullName))
    {
        Console.WriteLine(a.FullName);
    }
    throw;
}
catch (Exception ex)
{
    Console.WriteLine("Unexpected exception while building the app: " + ex.Message);
    Console.WriteLine(ex.StackTrace);
    throw;
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

// Enable Swagger middleware (development only)
if (app.Environment.IsDevelopment())
{
    // Only enable middleware if the Swagger services were successfully registered.
    if (swaggerRegistered)
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    else
    {
        Console.WriteLine("Swagger middleware skipped because AddSwaggerGen failed at startup.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<RequestIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.MapControllers();

app.Run();
