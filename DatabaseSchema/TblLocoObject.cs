using OpenLoco.Dat.Data;
using OpenLoco.Dat.Objects;

namespace OpenLoco.Db.Schema
{
	public class TblLocoObject
	{
		public int TblLocoObjectId { get; set; }

		public string Name { get; set; }

		#region OriginalDatdata

		public string OriginalName { get; set; }

		public uint OriginalChecksum { get; set; }

		public byte[] OriginalBytes { get; set; }

		#endregion

		public SourceGame SourceGame { get; set; }

		public ObjectType ObjectType { get; set; }

		public VehicleType? VehicleType { get; set; }

		#region Metadata

		public string? Description { get; set; }

		public TblAuthor? Author { get; set; }

		public DateTime? CreationDate { get; set; }

		public DateTime? LastEditDate { get; set; }

		public ICollection<TblTag> Tags { get; set; }

		public ICollection<TblModpack> Modpacks { get; set; }

		public ObjectAvailability Availability { get; set; }

		public TblLicence? Licence { get; set; }

		#endregion
	}
}
