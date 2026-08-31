using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TaskBridge_API.Common;
using TaskBridge_API.Data;
using TaskBridge_API.Notifications;
using TaskBridge_API.Projects;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<TaskBridgeDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("TaskBridge")));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ICurrentTenantProvider, HttpContextTenantProvider>();
builder.Services.AddScoped<ICurrentUserProvider, HttpContextUserProvider>();

builder.Services.AddScoped<IAuditRepository, AuditRepository>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ITeamMembershipRepository, TeamMembershipRepository>();
builder.Services.AddScoped<IProjectEventNotifier, ProjectEventNotifier>();

var jwtSigningKey = builder.Configuration["Jwt:SigningKey"]
    ?? throw new InvalidOperationException("Jwt:SigningKey configuration is required (set via user-secrets/env var in non-dev environments).");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey)),
            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Internal service-to-service calls (e.g. Project Service -> Audit) carry a "scope=internal" claim, not a normal user token.
    options.AddPolicy("InternalService", policy => policy.RequireClaim("scope", "internal"));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    // Dev convenience only; replace with EF Core migrations before production use.
    var dbContext = scope.ServiceProvider.GetRequiredService<TaskBridgeDbContext>();
    dbContext.Database.EnsureCreated();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
