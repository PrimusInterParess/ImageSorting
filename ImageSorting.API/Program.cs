using ImageSorting.Core;
using ImageSorting.Core.Interfaces;
using ImageSorting.Core.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

builder.Services.Configure<SortingOptions>(builder.Configuration.GetSection("Sorting"));
builder.Services.AddScoped<IImageSortingService, ImageSortingService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
