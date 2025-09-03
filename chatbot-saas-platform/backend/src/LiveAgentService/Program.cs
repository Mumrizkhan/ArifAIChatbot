using LiveAgentService.Hubs;
using LiveAgentService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Shared.Application.Common.Interfaces;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Messaging;
using Shared.Infrastructure.Persistence;
using StackExchange.Redis;
using System.Text;
using AnalyticsService.Services;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();
var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your valid token in the text input below.\nExample: \"Bearer eyJhbGciOiJIUzI1NiIs...\""
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);
}).AddStackExchangeRedis(builder.Configuration.GetValue<string>("Redis:ConnectionString") ?? "", options =>
     {
         options.Configuration.ChannelPrefix = "agentHub";
     });

// Configure for proxy
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                             ForwardedHeaders.XForwardedHost |
                             ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddHttpClient<IChatRuntimeIntegrationService, ChatRuntimeIntegrationService>();
builder.Services.AddScoped<IChatRuntimeIntegrationService, ChatRuntimeIntegrationService>();
builder.Services.AddScoped<IAgentRoutingService, AgentRoutingService>();
builder.Services.AddScoped<IQueueManagementService, QueueManagementService>();
builder.Services.AddScoped<IAgentManagementService, AgentManagementService>();
builder.Services.AddScoped<IConversationRatingService, ConversationRatingService>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(60)
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/agentHub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddScoped<ILiveAgentAnalyticsService, LiveAgentAnalyticsService>();
builder.Services.AddScoped<IAnalyticsMessageBusService, AnalyticsMessageBusService>();

// Message Bus
builder.Services.AddSingleton<IMessageBus, RabbitMQMessageBus>();



builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        var allowedOrigins = new[] {
             "https://api-stg-arif.tetco.sa",
            "https://chatbot-stg-arif.tetco.sa",
            "https://agent-stg-arif.tetco.sa",
            "https://admin-stg-arif.tetco.sa",
            "https://tenant-stg-arif.tetco.sa",
            "http://localhost:5173",
            "http://localhost:5174",
            "http://localhost:5175",
            "http://localhost:8000"
        };

        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseRouting();
app.UseCors("AllowAll");

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UseWebSockets();

app.MapHub<AgentHub>("/agentHub").RequireCors("AllowAll");
//app.MapHub<AgentHub>("agent/agentHub").RequireCors("AllowAll");

// Initialize Analytics Message Bus
using (var scope = app.Services.CreateScope())
{
    var analyticsMessageBus = scope.ServiceProvider.GetRequiredService<IAnalyticsMessageBusService>();
    analyticsMessageBus.InitializeSubscriptions();
}

app.Run();



