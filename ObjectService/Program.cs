using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using OpenLoco.Definitions.Database;
using OpenLoco.Definitions.Web;
using OpenLoco.ObjectService;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var connectionString = builder.Configuration.GetConnectionString("SQLiteConnection");

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
	var provider = builder.Services.BuildServiceProvider().GetRequiredService<IApiVersionDescriptionProvider>();
	foreach (var description in provider.ApiVersionDescriptions)
	{
		options.SwaggerDoc(
			description.GroupName,
			new OpenApiInfo()
			{
				Title = $"ObjectService {description.ApiVersion}",
				Version = description.ApiVersion.ToString(),
			});
	}
	options.SwaggerDoc(
		"(pre-versioning)",
		new OpenApiInfo()
		{
			Title = "ObjectService (pre-versioning)",
			Version = "0.0",
		});
});
builder.Services.AddDbContext<LocoDb>(opt => opt.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSingleton<Server>();
_ = builder.Services.Configure<ServerSettings>(builder.Configuration.GetSection("ObjectService"));
builder.Services.AddHttpLogging(logging =>
{
	logging.LoggingFields = HttpLoggingFields.ResponsePropertiesAndHeaders
		| HttpLoggingFields.Duration; // this is `All` excluding `ResponseBody`
	logging.CombineLogs = true;
});

var tokenPolicy = "token";
var myOptions = new RateLimitOptions();
builder.Configuration.GetSection(RateLimitOptions.Name).Bind(myOptions);

builder.Services.AddRateLimiter(rlOptions => rlOptions
	.AddTokenBucketLimiter(policyName: tokenPolicy, options =>
	{
		options.TokenLimit = myOptions.TokenLimit;
		options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
		options.QueueLimit = myOptions.QueueLimit;
		options.ReplenishmentPeriod = TimeSpan.FromSeconds(myOptions.ReplenishmentPeriod);
		options.TokensPerPeriod = myOptions.TokensReplenishedPerPeriod;
		options.AutoReplenishment = myOptions.AutoReplenishment;
		rlOptions.OnRejected = (context, cancellationToken) =>
		{
			if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
			{
				context.HttpContext.Response.Headers.RetryAfter = retryAfter.TotalSeconds.ToString();
			}

			context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
			_ = context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.", cancellationToken);

			return new ValueTask();
		};
	}));

_ = builder.Services.AddApiVersioning(options =>
{
	options.ReportApiVersions = true;
	options.DefaultApiVersion = new ApiVersion(2, 0);
	options.AssumeDefaultVersionWhenUnspecified = true;
	options.ApiVersionReader = ApiVersionReader.Combine(
		new UrlSegmentApiVersionReader());
}).AddApiExplorer(options =>
{
	options.GroupNameFormat = "'v'VVV";
	options.SubstituteApiVersionInUrl = true;
});

var app = builder.Build();
app.UseHttpLogging();
app.UseRateLimiter();

var objRoot = builder.Configuration["ObjectService:RootFolder"];
ArgumentException.ThrowIfNullOrEmpty(objRoot, nameof(objRoot));
var server = new Server(new ServerSettings(objRoot) { RootFolder = objRoot! });

var apiSet = app.NewApiVersionSet().Build();

var groupDeprecated = app
	.MapGroup("")
	.RequireRateLimiting(tokenPolicy);

var groupVersioned = app
	.MapGroup("v{version:apiVersion}")
	.WithApiVersionSet(apiSet)
	//.HasDeprecatedApiVersion(1.0)
	.HasApiVersion(1.0)
	.RequireRateLimiting(tokenPolicy);

MapRoutes(groupDeprecated, server);
MapRoutes(groupVersioned, server);

if (app.Environment.IsDevelopment())
{
	_ = app.UseSwagger();
	_ = app.UseSwaggerUI(
		options =>
		{
			foreach (var description in app.DescribeApiVersions())
			{
				var url = $"/swagger/{description.GroupName}/swagger.json";
				var name = description.GroupName.ToUpperInvariant();
				options.SwaggerEndpoint(url, name);
			}
		});
}

app.Run();

static void MapRoutes(RouteGroupBuilder routeGroup, Server server)
{
	// GET
	_ = routeGroup.MapGet(Routes.ListObjects, Server.ListObjects);

	_ = routeGroup.MapGet(Routes.GetDat, server.GetDat);
	_ = routeGroup.MapGet(Routes.GetObject, server.GetObject);
	_ = routeGroup.MapGet(Routes.GetDatFile, server.GetDatFile);
	_ = routeGroup.MapGet(Routes.GetObjectFile, server.GetObjectFile);
	_ = routeGroup.MapGet(Routes.ListScenarios, server.ListScenarios);
	_ = routeGroup.MapGet(Routes.GetScenario, server.GetScenario);

	// POST
	_ = routeGroup.MapPost(Routes.UploadDat, server.UploadDat);
	_ = routeGroup.MapPost(Routes.UploadObject, server.UploadObject);

	// PATCH
	_ = routeGroup.MapPatch(Routes.UpdateDat, server.UpdateDat);
	_ = routeGroup.MapPatch(Routes.UpdateObject, server.UpdateObject);
}
