using Definitions;

namespace OpenLoco.Definitions.Database
{
	public interface IDbReferenceData : IHasId
	{
		public string Name { get; set; }
	}
}
