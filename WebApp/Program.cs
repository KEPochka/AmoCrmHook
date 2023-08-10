using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using WebApp;
using WebApp.DataContext;
using WebApp.DataLoader;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultDatabase");

builder.Services.AddSingleton<IDbContextOptions>(new DbContextOptionsBuilder<ApplicationDbContext>()
				.UseNpgsql(connectionString)
				.Options);

builder.Services.AddSingleton<IMetaDataEditor, MetaDataEditor>();

var settings = builder.Configuration.GetSection("Settings").Get<Settings>();
builder.Services.AddSingleton(settings);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();
app.Run();
