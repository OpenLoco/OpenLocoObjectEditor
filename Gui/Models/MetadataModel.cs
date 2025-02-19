using Definitions.Database.Objects;
using OpenLoco.Definitions;
using OpenLoco.Definitions.Database;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace OpenLoco.Gui.Models
{
	public class MetadataModel(string uniqueName, string datName, uint datChecksum)
	{
		public string UniqueName { get; init; } = uniqueName;

		public string DatName { get; init; } = datName;

		public uint DatChecksum { get; init; } = datChecksum;

		public string? Description { get; set; }

		[Browsable(false)]
		public ICollection<TblAuthor> Authors { get; set; }

		public DateTimeOffset? CreationDate { get; set; }

		public DateTimeOffset? LastEditDate { get; set; }

		public DateTimeOffset UploadDate { get; set; }

		[Browsable(false)]

		public ICollection<TblTag> Tags { get; set; }

		[Browsable(false)]
		public ICollection<TblLocoObjectPack> ObjectPacks { get; set; }

		public ObjectAvailability Availability { get; set; }

		public TblLicence? Licence { get; set; }
	}
}
