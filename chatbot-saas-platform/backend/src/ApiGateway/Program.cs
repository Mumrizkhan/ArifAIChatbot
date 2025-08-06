using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Microsoft.OpenApi.Models; // Ensure this namespace is correct
using Microsoft.OpenApi; // This might not be necessary, consider removing if unused
using Serilog;


Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();
var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();



// Add services to the container.
builder.Services.AddOpenApi();

// Add Swagger services
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "API Gateway", Version = "v1" }); // Ensure OpenApiInfo is correctly referenced
});

// Add health checks
builder.Services.AddHealthChecks();

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
builder.Services.AddOcelot();

// Add CORS services
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        
        var allowedHosts = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        policy
            .WithOrigins(allowedHosts)//.AllowAnyOrigin()    // For development; restrict in production!
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();
app.UseCors("AllowAll");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Gateway v1");

        // Underlying service Swagger endpoints
        c.SwaggerEndpoint("http://localhost:5000/swagger/v1/swagger.json", "Identity Service");
        c.SwaggerEndpoint("http://localhost:5001/swagger/v1/swagger.json", "AI Orchestration Service");
        c.SwaggerEndpoint("http://localhost:5002/swagger/v1/swagger.json", "Chat Runtime Service");
        c.SwaggerEndpoint("http://localhost:5003/swagger/v1/swagger.json", "Live Agent Service");
        c.SwaggerEndpoint("http://localhost:5004/swagger/v1/swagger.json", "Notification Service");
        c.SwaggerEndpoint("http://localhost:5005/swagger/v1/swagger.json", "Workflow Service");
        c.SwaggerEndpoint("http://localhost:5006/swagger/v1/swagger.json", "Analytics Service");
        c.SwaggerEndpoint("http://localhost:5007/swagger/v1/swagger.json", "Tenant Management Service");
        c.SwaggerEndpoint("http://localhost:5008/swagger/v1/swagger.json", "Subscription Service");
        c.SwaggerEndpoint("http://localhost:5009/swagger/v1/swagger.json", "Knowledgebase Service");

        c.RoutePrefix = "swagger";
    });
}
app.UseHttpsRedirection();

app.UseRouting();

// Add custom exception handling and logging middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<LoggingMiddleware>();

// Map health check endpoint
app.MapHealthChecks("/health");

app.UseEndpoints(endpoints => { });
app.UseWebSockets();//For SignalR support in Chat Runtime Service
await app.UseOcelot();

app.Run();
  
