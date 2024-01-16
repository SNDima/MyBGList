using Asp.Versioning;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
	options.SwaggerDoc("v1", new OpenApiInfo { Title = "MyBGList", Version = "v1.0" });
	options.SwaggerDoc("v2", new OpenApiInfo { Title = "MyBGList", Version = "v2.0" });
});

builder.Services.AddCors(options =>
{
	options.AddDefaultPolicy(cfg =>
	{
		cfg.WithOrigins(builder.Configuration["AllowedOrigins"]);
		cfg.AllowAnyHeader();
		cfg.AllowAnyMethod();
	});
	options.AddPolicy(name: "AnyOrigin", cfg =>
	{
		cfg.AllowAnyOrigin();
		cfg.AllowAnyHeader();
		cfg.AllowAnyMethod();
	});
});

builder.Services.AddApiVersioning(options =>
{
	options.ApiVersionReader = new UrlSegmentApiVersionReader();
	options.AssumeDefaultVersionWhenUnspecified = true;
	options.DefaultApiVersion = new ApiVersion(1, 0);
})
	.AddApiExplorer(options =>
	{
		options.GroupNameFormat = "'v'VVV";
		options.SubstituteApiVersionInUrl = true;
	});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Configuration.GetValue<bool>("UseSwagger"))
{
	app.UseSwagger();
	app.UseSwaggerUI(options =>
	{
		options.SwaggerEndpoint("/swagger/v1/swagger.json", "MyBGList v1");
		options.SwaggerEndpoint("/swagger/v2/swagger.json", "MyBGList v2");
	});
}

if (app.Configuration.GetValue<bool>("UseDeveloperExceptionPage"))
	app.UseDeveloperExceptionPage();
else
	app.UseExceptionHandler("/error");

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthorization();

app.MapGet("/v{version:ApiVersion}/error",
	[ApiVersion("1.0")]
	[ApiVersion("2.0")]
	[EnableCors("AnyOrigin")]
	[ResponseCache(NoStore = true)] () =>
	Results.Problem());
app.MapGet("/v{version:ApiVersion}/error/test",
	[ApiVersion("1.0")]
	[ApiVersion("2.0")]
	[EnableCors("AnyOrigin")]
	[ResponseCache(NoStore = true)] () =>
	{ throw new Exception("test"); })
	.RequireCors("AnyOrigin");
app.MapGet("/v{version:ApiVersion}/cod/test",
	[ApiVersion("1.0")]
	[ApiVersion("2.0")]
	[EnableCors("AnyOrigin")]
	[ResponseCache(NoStore = true)] () =>
	Results.Text("<script>" +
		"window.alert('Your client supports JavaScript!" +
		"\\r\\n\\r\\n" +
		$"Server time (UTC): {DateTime.UtcNow:o}" +
		"\\r\\n" +
		"Client time (UTC): ' + new Date().toISOString());" +
		"</script>" +
		"<noscript>Your client does not support JavaScript</noscript>",
		"text/html"));

app.MapControllers();

app.Run();
