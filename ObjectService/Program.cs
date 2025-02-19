using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using OpenLoco.Definitions.Database;
using OpenLoco.ObjectService;
using Scalar.AspNetCore;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var connectionString = builder.Configuration.GetConnectionString("SQLiteConnection");

builder.Services.AddOpenApi();
_ = builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks();
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

var rateLimiterSection = builder.Configuration.GetSection("ObjectService:RateLimiter");
ArgumentNullException.ThrowIfNull(rateLimiterSection);
var rateLimiter = new RateLimitOptions();
rateLimiterSection.Bind(rateLimiter);

builder.Services.AddRateLimiter(rlOptions => rlOptions
	.AddTokenBucketLimiter(policyName: tokenPolicy, options =>
	{
		options.TokenLimit = rateLimiter.TokenLimit;
		options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
		options.QueueLimit = rateLimiter.QueueLimit;
		options.ReplenishmentPeriod = TimeSpan.FromSeconds(rateLimiter.ReplenishmentPeriod);
		options.TokensPerPeriod = rateLimiter.TokensReplenishedPerPeriod;
		options.AutoReplenishment = rateLimiter.AutoReplenishment;
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

var app = builder.Build();
app.UseHttpLogging();
app.UseRateLimiter();

var objRoot = builder.Configuration["ObjectService:RootFolder"];
var paletteMapFile = builder.Configuration["ObjectService:PaletteMapFile"];
ArgumentNullException.ThrowIfNull(objRoot);
ArgumentNullException.ThrowIfNull(paletteMapFile);
var server = new Server(new ServerSettings(objRoot, paletteMapFile));

_ = app
	.MapServerRoutes(server)
	.MapHealthChecks("/health")
	.RequireRateLimiting(tokenPolicy);

var showSwagger = builder.Configuration.GetValue<bool?>("ObjectService:ShowSwagger");
ArgumentNullException.ThrowIfNull(showSwagger);
if (showSwagger == true)
{
	_ = app.MapOpenApi();
	_ = app.MapScalarApiReference(options =>
	{
		_ = options
		.WithTitle("OpenLoco Object Service")
		.WithTheme(ScalarTheme.Solarized)
		.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
	});
}

app.Run();
