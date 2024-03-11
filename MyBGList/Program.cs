using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MyBGList.Constants;
using MyBGList.GraphQL;
using MyBGList.gRPC;
using MyBGList.Models;
using MyBGList.Swagger;
using Serilog;
using Serilog.Sinks.MSSqlServer;
using Swashbuckle.AspNetCore.Annotations;
using System.Reflection;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Logging
    .ClearProviders()
    .AddSimpleConsole()
    .AddDebug();
//.AddApplicationInsights(telemetry =>
//	telemetry.ConnectionString = builder.Configuration["Azure:ApplicationInsights:ConnectionString"],
//	loggerOptions => { }
//);

builder.Host.UseSerilog((ctx, lc) =>
{
    lc.ReadFrom.Configuration(ctx.Configuration);
    lc.Enrich.WithMachineName();
    lc.Enrich.WithThreadId();
    lc.WriteTo.File("Logs/log.txt",
        outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] [{MachineName} #{ThreadId}] " +
            "{Message:lj}{NewLine}{Exception}",
        rollingInterval: RollingInterval.Day);
    lc.WriteTo.MSSqlServer(connectionString:
        ctx.Configuration.GetConnectionString("DefaultConnection"),
        sinkOptions: new MSSqlServerSinkOptions
        {
            TableName = "LogEvents",
            AutoCreateSqlTable = true
        },
        columnOptions: new ColumnOptions
        {
            AdditionalColumns = [
                new SqlColumn
                {
                    ColumnName = "SourceContext",
                    PropertyName = "SourceContext",
                    DataType = System.Data.SqlDbType.NVarChar
                }
            ]
        }
        );
}, writeToProviders: true);

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

    options.CacheProfiles.Add("NoCache", new CacheProfile { NoStore = true });
    options.CacheProfiles.Add("Any-60", new CacheProfile
    {
        Location = ResponseCacheLocation.Any,
        Duration = 60
    });
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.EnableAnnotations();

    var xmlFilename = $"{Assembly.GetExecutingAssembly()
        .GetName().Name}.xml";
    options.IncludeXmlComments(System.IO.Path.Combine(
        AppContext.BaseDirectory, xmlFilename));

    options.ParameterFilter<SortColumnFilter>();
    options.ParameterFilter<SortOrderFilter>();

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "bearer"
    });

    //options.AddSecurityRequirement(new OpenApiSecurityRequirement
    //{
    //    {
    //        new OpenApiSecurityScheme
    //        {
    //            Reference = new OpenApiReference
    //            {
    //                Type = ReferenceType.SecurityScheme,
    //                Id = "Bearer"
    //            }
    //        },
    //        Array.Empty<string>()
    //    }
    //});
    options.OperationFilter<AuthRequirementFilter>();
    options.DocumentFilter<CustomDocumentFilter>();
    options.RequestBodyFilter<PasswordRequestFilter>();
    options.SchemaFilter<CustomKeyValueFilter>();
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

builder.Services.AddGraphQLServer()
    .AddAuthorization()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddProjections()
    .AddFiltering()
    .AddSorting();

builder.Services.AddGrpc();

builder.Services.AddIdentity<ApiUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 12;
}).AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme =
    options.DefaultChallengeScheme =
    options.DefaultForbidScheme =
    options.DefaultScheme =
    options.DefaultSignInScheme =
    options.DefaultSignOutScheme =
        JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["JWT:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["JWT:Audience"],
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
          System.Text.Encoding.UTF8.GetBytes(
              builder.Configuration["JWT:SigningKey"]))
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ModeratorWithMobilePhone", policy =>
    {
        policy.RequireClaim(ClaimTypes.Role, RoleNames.Moderator)
            .RequireClaim(ClaimTypes.MobilePhone);
    });

    options.AddPolicy("MinAge18", policy =>
        policy.RequireAssertion(ctx => ctx.User.HasClaim(c => c.Type == ClaimTypes.DateOfBirth)
                && DateTime.ParseExact("yyyyMMdd",
                    ctx.User.Claims.First(c =>
                           c.Type == ClaimTypes.DateOfBirth).Value,
                    System.Globalization.CultureInfo.InvariantCulture)
                    >= DateTime.Now.AddYears(-18)));
});

// Code replaced by the [ManualValidationFilter] attribute
// builder.Services.Configure<ApiBehaviorOptions>(options =>
//	 options.SuppressModelStateInvalidFilter = true);

builder.Services.AddResponseCaching(options =>
{
    options.MaximumBodySize = 32 * 1024 * 1024; // 32 MB
    options.SizeLimit = 50 * 1024 * 1024; // 50 MB
});

builder.Services.AddMemoryCache();

//builder.Services.AddDistributedSqlServerCache(options =>
//{
//	options.ConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
//	options.SchemaName = "dbo";
//	options.TableName = "AppCache";
//});
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:ConnectionString"];
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Configuration.GetValue<bool>("UseSwagger"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    // HTTP Security Headers
    app.UseHsts();
    app.Use(async (context, next) =>
    {
        context.Response.Headers.Append("X-Frame-Options", "sameorigin");
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("Content-Security-Policy", "default-src ' self' ;");
        context.Response.Headers.Append("Referrer-Policy", "strict-origin");
        await next();
    });
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

app.UseResponseCaching();

app.UseAuthentication();
app.UseAuthorization();

app.MapGraphQL();
app.MapGrpcService<GrpcService>();

app.Use((context, next) =>
{
    context.Response.GetTypedHeaders().CacheControl =
        new Microsoft.Net.Http.Headers.CacheControlHeaderValue
        {
            NoCache = true,
            NoStore = true
        };
    return next.Invoke();
});

app.MapGet("/error",
    [EnableCors("AnyOrigin")]
[ResponseCache(CacheProfileName = "NoCache")] (HttpContext context) =>
    {
        var exceptionHandler =
            context.Features.Get<IExceptionHandlerPathFeature>();

        var details = new ProblemDetails();
        details.Detail = exceptionHandler?.Error.Message;
        details.Extensions["traceId"] =
            System.Diagnostics.Activity.Current?.Id
              ?? context.TraceIdentifier;
        details.Type =
            "https://tools.ietf.org/html/rfc7231#section-6.6.1";
        details.Status = StatusCodes.Status500InternalServerError;

        app.Logger.LogError(CustomLogEvents.Error_Get,
            exceptionHandler?.Error, "An unhandled exception occured.");

        return Results.Problem(details);
    });

app.MapGet("/error/test",
    [EnableCors("AnyOrigin")]
[ResponseCache(CacheProfileName = "NoCache")] () =>
        { throw new Exception("test"); })
    .RequireCors("AnyOrigin");

app.MapGet("/cod/test",
    [EnableCors("AnyOrigin")]
[ResponseCache(CacheProfileName = "NoCache")] () =>
    Results.Text("<script>" +
        "window.alert('Your client supports JavaScript!" +
        "\\r\\n\\r\\n" +
        $"Server time (UTC): {DateTime.UtcNow:o}" +
        "\\r\\n" +
        "Client time (UTC): ' + new Date().toISOString());" +
        "</script>" +
        "<noscript>Your client does not support JavaScript</noscript>",
        "text/html"));

app.MapGet("/cache/test/1",
    [EnableCors("AnyOrigin")]
(HttpContext context) =>
    {
        context.Response.Headers["cache-control"] = "no-cache, no-store";
        return Results.Ok();
    });

app.MapGet("/cache/test/2",
    [EnableCors("AnyOrigin")]
(HttpContext context) =>
    {
        return Results.Ok();
    });

app.MapGet("/auth/test/1",
    [Authorize]
[EnableCors("AnyOrigin")]
[SwaggerOperation(
    Tags = ["Auth"],
    Summary = "Auth test #1 (authenticated users).",
    Description = "Returns 200 - OK if called by " +
    "an authenticated user regardless of its role(s).")]
[SwaggerResponse(StatusCodes.Status200OK, "Authorized")]
[SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authorized")]
[ResponseCache(NoStore = true)] () =>
    {
        return Results.Ok("You are authorized!");
    });

app.MapGet("/auth/test/2",
    [Authorize(Roles = RoleNames.Moderator)]
[EnableCors("AnyOrigin")]
[SwaggerOperation(
    Tags = ["Auth"],
    Summary = "Auth test #2 (Moderator role).",
    Description = "Returns 200 - OK if called by " +
    "an authenticated user assigned to the Moderator role.")]
[ResponseCache(NoStore = true)] () =>
    {
        return Results.Ok("You are authorized!");
    });

app.MapGet("/auth/test/3",
    [Authorize(Roles = RoleNames.Administrator)]
[EnableCors("AnyOrigin")]
[SwaggerOperation(
    Tags = ["Auth"],
    Summary = "Auth test #3 (Administrator role).",
    Description = "Returns 200 - OK if called by " +
    "an authenticated user assigned to the Administrator role.")]
[ResponseCache(NoStore = true)] () =>
    {
        return Results.Ok("You are authorized!");
    });

app.MapControllers();

app.Run();
