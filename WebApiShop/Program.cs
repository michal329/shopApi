using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Entities.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NLog.Web;
using Repositories;
using Services;
using WebApiShop.Auth;
using WebApiShop.MiddleWare;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserServices, UserServices>();
builder.Services.AddScoped<IPasswordServices, PasswordServices>();
builder.Services.AddScoped<IProductsServices, ProductsServices>();
builder.Services.AddScoped<IProductsRepository, ProductsRepository>();
builder.Services.AddScoped<ICategoriesServices, CategoriesServices>();
builder.Services.AddScoped<ICategoriesRepository, CategoriesRepository>();
builder.Services.AddScoped<IOrdersRepository, OrdersRepository>();
builder.Services.AddScoped<IOrdersServices, OrdersServices>();
builder.Services.AddScoped<IRatingRepository, RatingRepository>();
builder.Services.AddScoped<IRatingService, RatingService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddSingleton<AuthCookie>();
builder.Services.AddSingleton(TimeProvider.System);

builder.Host.UseNLog();

builder.Services.AddDbContext<ApiShopContext>(option =>
    option.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ---------- Cache: Redis when configured, otherwise in-process memory ----------
builder.Services.Configure<CacheSettings>(builder.Configuration.GetSection(CacheSettings.SectionName));
string? redisConnection = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrWhiteSpace(redisConnection))
    builder.Services.AddStackExchangeRedisCache(options => options.Configuration = redisConnection);
else
    builder.Services.AddDistributedMemoryCache();
builder.Services.AddSingleton<ICacheService, DistributedCacheService>();

// ---------- Authentication: JWT from an HTTP-only cookie (or a Bearer header) ----------
IConfigurationSection jwtSection = builder.Configuration.GetSection(JwtSettings.SectionName);
builder.Services.Configure<JwtSettings>(jwtSection);
JwtSettings jwt = jwtSection.Get<JwtSettings>() ?? new JwtSettings();
if (Encoding.UTF8.GetByteCount(jwt.Key) < 32)
    throw new InvalidOperationException(
        "Jwt:Key must be set to at least 32 bytes (appsettings.Development.json locally, or the Jwt__Key environment variable).");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Keep the claim names exactly as issued ("sub", "role", ...).
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = ShopClaims.UserId,
            RoleClaimType = ShopClaims.Role,
        };
        options.Events = new JwtBearerEvents
        {
            // Browsers send the token in the cookie; Swagger and other tools
            // send "Authorization: Bearer ...". The header wins if both exist.
            OnMessageReceived = context =>
            {
                if (!context.Request.Headers.ContainsKey("Authorization")
                    && context.Request.Cookies.TryGetValue(jwt.CookieName, out string? token))
                {
                    context.Token = token;
                }
                return Task.CompletedTask;
            },
        };
    });

// Deny by default: every endpoint needs a signed-in user unless it is
// marked [AllowAnonymous], so a new endpoint can't be left open by mistake.
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

// ---------- Rate limiting: sliding window per client IP ----------
SlidingWindowRateLimiterOptions SlidingWindow(string section)
{
    IConfigurationSection s = builder.Configuration.GetSection($"RateLimiting:{section}");
    return new SlidingWindowRateLimiterOptions
    {
        PermitLimit = s.GetValue("PermitLimit", 100),
        Window = TimeSpan.FromSeconds(s.GetValue("WindowSeconds", 60)),
        SegmentsPerWindow = s.GetValue("SegmentsPerWindow", 6),
        QueueLimit = 0,
    };
}
string ClientIp(HttpContext context) => context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Every request counts toward the client's global limit.
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetSlidingWindowLimiter(ClientIp(context), _ => SlidingWindow("Global")));

    // Login and registration get a much tighter limit against password guessing.
    options.AddPolicy(RateLimitPolicies.Auth, context =>
        RateLimitPartition.GetSlidingWindowLimiter(ClientIp(context), _ => SlidingWindow("Auth")));
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddCors(options => {
    options.AddPolicy("AllowAngular", policy => {
        policy.WithOrigins(
                "http://localhost:4200",
                "https://localhost:4200"
              )
              .AllowAnyHeader()
              .AllowAnyMethod()
              // Lets the Angular app send the auth cookie (withCredentials: true).
              .AllowCredentials();
    });
});

// ---------- Swagger, with an "Authorize" button for a Bearer token ----------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Shop API", Version = "v1" });
    var bearer = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Not needed in this page: logging in with POST /api/User/Login sets the auth cookie, " +
                      "which the browser sends with every call. To test as another user, paste a token here " +
                      "(copy it from the shop_access_token cookie in the browser's DevTools > Application > Cookies).",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
    };
    options.AddSecurityDefinition("Bearer", bearer);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement { [bearer] = Array.Empty<string>() });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Shop API v1");
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAngular");
app.UseErrorHandling();
// Before UseRating, so rejected requests aren't written to the database.
app.UseRateLimiter();
app.UseRating();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
