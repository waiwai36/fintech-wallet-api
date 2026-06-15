using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json;
using wallet.DALs;
using wallet.DALs.Interfaces;
using wallet.Data;
using wallet.Exceptions;
using wallet.Helpers;
using wallet.Middleware;
using wallet.Models.Responses;
using wallet.Services;
using wallet.Services.Infrastructure;
using wallet.Services.PaymentGateway;

var builder = WebApplication.CreateBuilder(args);

#region CONTROLLERS

builder.Services.AddControllers();

#endregion

#region DB CONTEXT

builder.Services.AddDbContext<WalletdbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Wallet"),
        sql =>
        {
            sql.CommandTimeout(180);
        });

    if (builder.Environment.IsDevelopment())
    {
        options.EnableDetailedErrors();
    }
});

#endregion

#region JWT + JWE AUTHENTICATION
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // dev only
    options.TokenValidationParameters = new TokenValidationParameters
    {

        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],

        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],

        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,

        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Convert.FromBase64String(builder.Configuration["Jwt:Key"])
        ),
        TokenDecryptionKey = new SymmetricSecurityKey(
           Convert.FromBase64String(builder.Configuration["Jwt:EncryptionKey"])
        )
    };
    options.Events = new JwtBearerEvents
    {
        OnChallenge = async context =>
        {
            context.HandleResponse(); // Clode Default response           
            var exception = new AppException(message: null!, StatusCodes.Status401Unauthorized);
            await HandleJwtErrorDirectlyAsync(context.HttpContext, exception);
        },
        OnForbidden = async context =>
        {
            var exception = new AppException(message: null!, StatusCodes.Status403Forbidden);
            await HandleJwtErrorDirectlyAsync(context.HttpContext, exception);
        }
    };
});

#endregion

#region AUTHORIZATION
builder.Services.AddAuthorization();
// custom policy system
builder.Services.AddSingleton<IAuthorizationPolicyProvider, DynamicPermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, DynamicPolicyHandler>();
#endregion

//DI Register
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IWalletRepository, WalletRepository>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IWalletValidator, WalletValidator>();
builder.Services.AddSingleton<IClock, SystemClock>();

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("StrictPolicy", opt =>
    {
        opt.PermitLimit = 5; 
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

#region API VERSIONING

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;

    options.ApiVersionReader = new UrlSegmentApiVersionReader();
});

builder.Services.AddVersionedApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV"; // v1, v2
    options.SubstituteApiVersionInUrl = true;
});

#endregion

#region SWAGGER

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    // JWT Auth in Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your token}"
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
            new string[] {}
        }
    });

    options.DescribeAllParametersInCamelCase();
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
});

#endregion

var app = builder.Build();

var apiVersionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
static async Task HandleJwtErrorDirectlyAsync(HttpContext context, Exception exception)
{   
    var (statusCode, message) = GlobalExceptionMiddleware.MapException(exception);

    if (string.IsNullOrWhiteSpace(message))
    {
        message = statusCode switch
        {
            StatusCodes.Status401Unauthorized => "Unauthorized access. Token is missing or invalid.",
            StatusCodes.Status403Forbidden => "Forbidden. You do not have permission to access this resource.",
            _ => "Access denied."
        };
    }

    context.Response.StatusCode = statusCode;
    context.Response.ContentType = "application/json";

    var response = ApiResponse<object?>.Fail(message);
    var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    await context.Response.WriteAsJsonAsync(response, jsonOptions);
}

#region PIPELINE

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    foreach (var description in apiVersionProvider.ApiVersionDescriptions)
    {
        options.SwaggerEndpoint(
            $"/swagger/{description.GroupName}/swagger.json",
            description.GroupName.ToUpperInvariant()
        );
    }

    options.RoutePrefix = "swagger"; // or "" for root
});

app.UseRateLimiter();

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.UseMiddleware<PolicyMiddleware>();

app.MapGet("/health", () => Results.Ok("Healthy"));

app.MapControllers();

#endregion

app.Run();
