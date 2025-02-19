namespace OpenLoco.ObjectService
{
	// https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-8.0
	// https://github.com/dotnet/AspNetCore.Docs.Samples/blob/main/fundamentals/middleware/rate-limit/WebRateLimitAuth/Models/MyRateLimitOptions.cs
	public class RateLimitOptions
	{
		public int ReplenishmentPeriod { get; set; } = 1;
		public int QueueLimit { get; set; } = 500;
		public int TokenLimit { get; set; } = 100;
		public int TokensReplenishedPerPeriod { get; set; } = 50;
		public bool AutoReplenishment { get; set; } = true;
	}
}
