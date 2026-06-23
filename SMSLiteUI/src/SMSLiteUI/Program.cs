using External.ELMA.Client.Configuration;
using External.ELMA.Client.Services;
using External.ELMA.Client.Services.Contracts.Integration;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using SMS.Integration.SurveyReview.Configuration;
using SMS.Integration.SurveyReview.Services;
using SMS.Integration.SurveyReview.Services.Contracts.Integration;
using SMSLiteUI;
using SMSLiteCommandAPI.Common.SMSLiteAuthentication;
using SMSLiteCommandAPI.Common.SMSLiteConfiguration;
using SMSLiteCommandAPI.Controllers.SMSLiteEndpoints;
using SMSLiteCommandAPI.Repositories;
using SMSLiteCommandAPI.Services;
using SMSLiteUI.Services;

var builder = WebApplication.CreateBuilder(args);
var MyAllowLocalhostOrigins = "_myAllowLocalhostOrigins";

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowLocalhostOrigins,
        policy =>
        {
            policy.WithOrigins(
                "https://localhost:7222",
                "http://localhost:7222",
                "https://localhost:5045",
                "http://localhost:5045"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
        });
});

// Options
builder.Services.Configure<ElmaClientOptions>(builder.Configuration.GetSection(ElmaClientOptions.SectionName));
builder.Services.Configure<SurveyReviewClientOptions>(builder.Configuration.GetSection(SurveyReviewClientOptions.SectionName));
builder.Services.Configure<SmsLiteDatabaseOptions>(builder.Configuration.GetSection(SmsLiteDatabaseOptions.SectionName));

// Blazor Server UI
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
CompatibilitySettings.UseDropDownInGridColumnChooser = true;
builder.Services.AddDevExpressBlazor();
builder.Services.AddScoped(sp =>
{
    var navigation = sp.GetRequiredService<NavigationManager>();
    return new HttpClient { BaseAddress = new Uri(navigation.BaseUri) };
});

// Auth
builder.Services.AddAuthentication()
    .AddScheme<AuthenticationSchemeOptions, ElmaClientAuthenticationHandler>(ElmaAuthenticationDefaults.SchemeName, _ => { });

builder.Services.AddAuthorization();

// Http Clients
builder.Services.AddHttpClient<ISurveyReviewDownstreamClient, HttpSurveyReviewDownstreamClient>();
builder.Services.AddHttpClient<IElmaDownstreamClient, StubElmaDownstreamClient>();

// Circuit breakers, gateways
builder.Services.AddSingleton<ISurveyReviewCircuitBreaker, InMemorySurveyReviewCircuitBreaker>();
builder.Services.AddSingleton<IElmaCircuitBreaker, InMemoryElmaCircuitBreaker>();
builder.Services.AddSingleton<ISurveyReviewGateway, ResilientSurveyReviewGateway>();
builder.Services.AddSingleton<IElmaGateway, ResilientElmaGateway>();
builder.Services.AddScoped<SurveyInstanceDao>();
builder.Services.AddScoped<SurveyInstanceService>();
builder.Services.AddScoped<BreadcrumbService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors(MyAllowLocalhostOrigins);
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();


// Map ALL endpoints here
app.MapSurveyInstanceEndpoints();
app.MapSurveyReviewEndpoints();
app.MapElmaClientEndpoints();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
