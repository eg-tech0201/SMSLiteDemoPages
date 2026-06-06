using External.ELMA.Client.Configuration;
using External.ELMA.Client.Services;
using Microsoft.AspNetCore.Authentication;
using SMS.Integration.SurveyReview.Configuration;
using SMS.Integration.SurveyReview.Services;
using SMSLiteStaticDemo.Server.Authentication;
using SMSLiteStaticDemo.Server.Endpoints;
using SMSLiteStaticDemo.Services;
using SMSLiteStaticDemo.Services.Contracts.Integration;

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

// Auth
builder.Services.AddAuthentication(ElmaAuthenticationDefaults.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, ElmaClientAuthenticationHandler>(ElmaAuthenticationDefaults.SchemeName, _ => { });

builder.Services.AddAuthorization();

// Http Clients
builder.Services.AddHttpClient<ISurveyReviewDownstreamClient, HttpSurveyReviewDownstreamClient>();
builder.Services.AddHttpClient<IElmaDownstreamClient, StubElmaDownstreamClient>();

// Circuit breakers, gateways
builder.Services.AddSingleton<SurveyInstanceService>();
builder.Services.AddSingleton<ISurveyReviewCircuitBreaker, InMemorySurveyReviewCircuitBreaker>();
builder.Services.AddSingleton<IElmaCircuitBreaker, InMemoryElmaCircuitBreaker>();
builder.Services.AddSingleton<ISurveyReviewGateway, ResilientSurveyReviewGateway>();
builder.Services.AddSingleton<IElmaGateway, ResilientElmaGateway>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseCors(MyAllowLocalhostOrigins);
app.UseAuthentication();
app.UseAuthorization();


// Map ALL endpoints here
app.MapSurveyInstanceEndpoints();
app.MapSurveyReviewEndpoints();
app.MapElmaClientEndpoints();

app.Run();