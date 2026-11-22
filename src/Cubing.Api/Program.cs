using Cubing.Api.Extensions;
using Cubing.Infrastructure;
using Cubing.Infrastructure.Data;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddCubing();
builder.Services.AddControllers();
builder.Services.AddOpenApi(options => options.AddCubingExamples());

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<CubingDbContext>().Database.EnsureCreated();
}

app.MapOpenApi();
app.MapScalarApiReference();
app.MapControllers();

app.Run();

// Makes Program visible to WebApplicationFactory in the test project.
public partial class Program;
