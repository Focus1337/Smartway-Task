using FileHub.Presentation;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

builder.AddCustomControllers();

builder.AddS3();
builder.AddCustomDb();

builder.AddCustomIdentity();
builder.AddCustomOpenIddict();

services.AddEndpointsApiExplorer();
builder.AddCustomSwaggerGen();

builder.AddCustomAuthentication();
services.AddAuthorization();

builder.AddCustomApplicationServices();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

await app.MigrateDbContext();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();