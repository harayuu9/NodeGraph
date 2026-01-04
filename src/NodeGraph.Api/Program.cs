using Microsoft.EntityFrameworkCore;
using NodeGraph.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Configure SQLite database
builder.Services.AddDbContext<GraphDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure CORS for browser access
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBrowser", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GraphDbContext>();
    db.Database.EnsureCreated();
}

// Configure the HTTP request pipeline
app.UseCors("AllowBrowser");

app.MapControllers();

app.Run();
