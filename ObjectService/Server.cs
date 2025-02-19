using Microsoft.Extensions.Options;
using ObjectService.TableHandlers;
using OpenLoco.Dat;

namespace OpenLoco.ObjectService
{
	// this is purely for the dependency--injection wrapper in Program.cs
	public static class ServerRouteBuilderExtensions
	{
		public static IEndpointRouteBuilder MapServerRoutes(
			this IEndpointRouteBuilder endpoints,
			Server server)
		{
			ArgumentNullException.ThrowIfNull(endpoints);
			return server.MapRoutes(endpoints);
		}
	}

	public class Server
	{
		public Server(ServerSettings settings)
		{
			Settings = settings;
			ServerFolderManager = new ServerFolderManager(Settings.RootFolder)!;
			ScenarioHandler = new(ServerFolderManager.ScenariosFolder);
			ObjectHandler = new(ServerFolderManager, new PaletteMap(Settings.PaletteMapFile));
		}

		public Server(IOptions<ServerSettings> options) : this(options.Value)
		{ }

		ServerSettings Settings { get; init; }

		ServerFolderManager ServerFolderManager { get; init; }

		public AuthorRequestHandler AuthorHandler { get; init; } = new();
		public TagRequestHandler TagHandler { get; init; } = new();
		public LicenceRequestHandler LicenceHandler { get; init; } = new();
		public ObjectPackRequestHandler ObjectPackHandler { get; init; } = new();
		public SC5FilePackRequestHandler SC5FilePackHandler { get; init; } = new();
		public ScenarioRequestHandler ScenarioHandler { get; init; }
		public ObjectRequestHandler ObjectHandler { get; init; }

		public IEndpointRouteBuilder MapRoutes(IEndpointRouteBuilder routeGroup)
		{
			AuthorHandler.MapRoutes(routeGroup);
			TagHandler.MapRoutes(routeGroup);
			LicenceHandler.MapRoutes(routeGroup);
			ObjectHandler.MapRoutes(routeGroup);
			ScenarioHandler.MapRoutes(routeGroup);
			SC5FilePackHandler.MapRoutes(routeGroup);
			ObjectPackHandler.MapRoutes(routeGroup);

			return routeGroup;
		}
	}
}
