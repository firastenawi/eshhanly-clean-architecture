using System.Globalization;
using System.Text.Json.Serialization;
using Eshhanly.Application;
using Eshhanly.Application.Common.Interfaces;
using Eshhanly.Infrastructure;
using Eshhanly.Web.Components;
using Eshhanly.Web.Endpoints;
using Eshhanly.Web.Services;
using Microsoft.OpenApi.Models;
using MudBlazor.Services;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, logger) => logger
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture));

// Layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddAuthorization();

// Web UI (Blazor, Interactive Server) + MudBlazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMudServices();

// REST API: enums as strings ("Type2", "PerHour") are friendlier for mobile clients.
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi(o => o.AddDocumentTransformer((document, _, _) =>
{
    document.Components ??= new OpenApiComponents();
    document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        Description = "accessToken from POST /api/v1/auth/login",
    };
    document.SecurityRequirements.Add(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = [],
    });
    return Task.CompletedTask;
}));

var app = builder.Build();

await app.InitializeDatabaseAsync();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapHealthChecks("/health");
app.MapOpenApi();               // /openapi/v1.json
app.MapScalarApiReference();    // /scalar/v1 — interactive API docs
app.MapApiEndpoints();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.RunAsync();
