using Entities.Models;
using Microsoft.EntityFrameworkCore;
using NLog.Web;
using Repositories;
using Services;
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

builder.Host.UseNLog();

// Bug 1 Fix: Read connection string from appsettings.json
builder.Services.AddDbContext<ApiShopContext>(option =>
    option.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();

// CORS: allow Angular on both http and https
builder.Services.AddCors(options => {
    options.AddPolicy("AllowAngular", policy => {
        policy.WithOrigins(
                "http://localhost:4200",
                "https://localhost:4200"
              )
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "My API V1");
    });
}

app.UseHttpsRedirection();

// Bug 4 Fix: correct middleware order
app.UseCors("AllowAngular");
app.UseErrorHandling();
app.UseRating();

app.UseStaticFiles();
app.UseAuthorization();
app.MapControllers();
app.Run();