using FileHub.Presentation;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
// const string controllersCorsPolicy = "ControllersPolicy";

builder.AddCustomControllers();

builder.AddCustomDb();
builder.AddCustomIdentity();
builder.AddCustomOpenIddict();

// builder.Services.AddCors(options =>
// {
//     options.AddPolicy(controllersCorsPolicy, policyBuilder =>
//     {
//         policyBuilder.WithOrigins("https://localhost:7298")
//             .WithMethods("GET", "POST", "PATCH")
//             .AllowAnyHeader()
//             .AllowCredentials();
//     });
// });

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

await app.MigrateDbContext();

// app.UseCors();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();//.RequireCors(controllersCorsPolicy);

app.Run();