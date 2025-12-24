using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.HttpOverrides;
using MusicData.Api.Endpoints;
using MusicData.Application;
using MusicData.Infrastructure;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddTelemetry(builder.Configuration);
builder.Services.AddFeatures();
builder.Services.AddDataContext(builder.Configuration);
builder.Services.AddServices(builder.Configuration);

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddIpRateLimiting(builder.Configuration);
builder.Services.AddResponseCaching();
builder.Services.AddApiAuthentication();

JsonSerializerOptions jsonOptions = new()
{
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
};
builder.Services.AddSingleton(jsonOptions);

builder.Services.ConfigureHttpJsonOptions(opts =>
{
    opts.SerializerOptions.PropertyNameCaseInsensitive = jsonOptions.PropertyNameCaseInsensitive;
    opts.SerializerOptions.PropertyNamingPolicy = jsonOptions.PropertyNamingPolicy;
    opts.SerializerOptions.DefaultIgnoreCondition = jsonOptions.DefaultIgnoreCondition;
});

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

ForwardedHeadersOptions forwardOptions = new()
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};

app.UseForwardedHeaders(forwardOptions);
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseResponseCaching();
app.UseIpRateLimiting();

app.MapArtistsApiV1();
app.MapAlbumsApiV1();
app.MapLyricsApiV1();
app.MapHealthChecks("/health");

await app.RunAsync();

