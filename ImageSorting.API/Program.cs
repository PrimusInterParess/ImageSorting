using Azure.Storage.Blobs;
using ImageSorting.Core;
using ImageSorting.Core.Interfaces;
using ImageSorting.Core.Options;
using ImageSorting.Core.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

builder.Services.Configure<SortingOptions>(builder.Configuration.GetSection("Sorting"));
builder.Services.AddScoped<IImageSortingService, ImageSortingService>();

var connectionString = builder.Configuration["AzureStorage:ConnectionString"];
if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddSingleton(new BlobServiceClient(connectionString));
}
else
{
    // If not configured, register a throwing factory to surface misconfig early
    builder.Services.AddSingleton<BlobServiceClient>(_ =>
        throw new InvalidOperationException("AzureStorage:ConnectionString is not configured."));
}
builder.Services.AddScoped<IBlobImageSortingService, BlobImageSortingService>();
builder.Services.AddScoped<IBlobUploadService, BlobUploadService>();
builder.Services.AddScoped<IBlobBrowseService, BlobBrowseService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();