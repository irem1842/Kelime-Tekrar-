using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);




builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactCors", policy =>
    {
        policy.WithOrigins("http://localhost:3000")  
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});


builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10485760; 
});


builder.Services.AddControllers();


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseCors("ReactCors");

app.UseAuthorization();
app.MapControllers();
app.Run();
