using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SMSLiteStaticDemo;
using SMSLiteStaticDemo.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Register ALL services your components inject
builder.Services.AddScoped<UserRecentSurveyService>();
builder.Services.AddScoped<SurveyInstanceService>();
builder.Services.AddScoped<DemoUserContextService>();
builder.Services.AddScoped<DemoAuditTrailService>();

await builder.Build().RunAsync();
