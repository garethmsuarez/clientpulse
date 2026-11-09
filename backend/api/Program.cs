using System.Text.Json.Serialization;
using ClientPulse.Api.Analysis;
using ClientPulse.Api.Assessments;
using ClientPulse.Api.Clients;
using ClientPulse.Api.Communications;
using ClientPulse.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Serialize enums such as CommunicationType by name ("Email") rather than number.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var connectionString = builder.Configuration.GetConnectionString("ClientPulse")
    ?? throw new InvalidOperationException("Connection string 'ClientPulse' is not configured.");

builder.Services.AddDbContext<ClientPulseDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddCommunicationAnalysis(builder.Configuration);
builder.Services.AddScoped<AssessmentService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapClientEndpoints();
app.MapCommunicationEndpoints();
app.MapAssessmentEndpoints();
app.MapAnalysisEndpoints();

app.Run();
