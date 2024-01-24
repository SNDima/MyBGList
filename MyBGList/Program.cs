using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBGList.Models;
using MyBGList.Swagger;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers(options =>
{
	options.ModelBindingMessageProvider.SetValueIsInvalidAccessor(
		(x) => $"The value '{x}' is invalid.");
	options.ModelBindingMessageProvider.SetValueMustBeANumberAccessor(
		(x) => $"The field {x} must be a number.");
	options.ModelBindingMessageProvider.SetAttemptedValueIsInvalidAccessor(
		(x, y) => $"The value '{x}' is not valid for {y}.");
	options.ModelBindingMessageProvider.SetMissingKeyOrValueAccessor(
		() => $"A value is required.");
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
	options.ParameterFilter<SortColumnFilter>();
	options.ParameterFilter<SortOrderFilter>();
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

builder.Services.AddDbContext<ApplicationDbContext>(options =>
	options.UseSqlServer(
		builder.Configuration.GetConnectionString("DefaultConnection"))
	);

// Code replaced by the [ManualValidationFilter] attribute
// builder.Services.Configure<ApiBehaviorOptions>(options =>
//	 options.SuppressModelStateInvalidFilter = true);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Configuration.GetValue<bool>("UseSwagger"))
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

if (app.Configuration.GetValue<bool>("UseDeveloperExceptionPage"))
	app.UseDeveloperExceptionPage();
else
	app.UseExceptionHandler("/error");
//app.UseExceptionHandler(action => {
//    action.Run(async context =>
//    {
//        var exceptionHandler =
//            context.Features.Get<IExceptionHandlerPathFeature>();

//        var details = new ProblemDetails();
//        details.Detail = exceptionHandler?.Error.Message;
//        details.Extensions["traceId"] =
//            System.Diagnostics.Activity.Current?.Id 
//              ?? context.TraceIdentifier;
//        details.Type =
//            "https://tools.ietf.org/html/rfc7231#section-6.6.1";
//        details.Status = StatusCodes.Status500InternalServerError;
//        await context.Response.WriteAsync(
//            System.Text.Json.JsonSerializer.Serialize(details));
//    });
//});

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthorization();

app.MapGet("/error",
	[EnableCors("AnyOrigin")]
[ResponseCache(NoStore = true)] (HttpContext context) =>
	{
		var exceptionHandler =
			context.Features.Get<IExceptionHandlerPathFeature>();

		var details = new ProblemDetails();
		details.Detail = exceptionHandler?.Error.Message;
		details.Extensions["traceId"] =
			System.Diagnostics.Activity.Current?.Id
			  ?? context.TraceIdentifier;

		if (exceptionHandler?.Error is NotImplementedException)
		{
			details.Type =
				"https://tools.ietf.org/html/rfc7231#section-6.6.2";
			details.Status = StatusCodes.Status501NotImplemented;
		}
		else if (exceptionHandler?.Error is TimeoutException)
		{
			details.Type =
				"https://tools.ietf.org/html/rfc7231#section-6.6.5";
			details.Status = StatusCodes.Status504GatewayTimeout;
		}
		else
		{
			details.Type =
				"https://tools.ietf.org/html/rfc7231#section-6.6.1";
			details.Status = StatusCodes.Status500InternalServerError;
		}

		return Results.Problem(details);
	});

app.MapGet("/error/test",
	[EnableCors("AnyOrigin")]
[ResponseCache(NoStore = true)] () =>
		{ throw new Exception("test"); })
	.RequireCors("AnyOrigin");

app.MapGet("/cod/test",
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
