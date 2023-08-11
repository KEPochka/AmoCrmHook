using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.OpenApi.Models;
using WebApp;
using WebApp.DataContext;
using WebApp.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultDatabase");

var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
	.UseNpgsql(connectionString)
	.Options;

builder.Services.AddSingleton<IDbContextOptions>(dbOptions);

builder.Services.AddSingleton<IMetaDataEditor, MetaDataEditor>();

var settings = builder.Configuration.GetSection("Settings").Get<Settings>();
builder.Services.AddSingleton(settings);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
	c.SwaggerDoc("v1", new OpenApiInfo { Title = "amoCRM Hook API", Version = "v1" });
	c.IncludeXmlComments(Path.Combine(System.AppContext.BaseDirectory, "WebApp.xml"));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();
app.Run();
