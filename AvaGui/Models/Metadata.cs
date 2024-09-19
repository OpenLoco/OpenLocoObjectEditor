using OpenLoco.Definitions;
using OpenLoco.Definitions.Database;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace AvaGui.Models
{
	public class MetadataModel
	{
		public MetadataModel(string datName, uint datChecksum)
		{
			DatName = datName;
			DatChecksum = datChecksum;
		}

		public string DatName { get; }
		public uint DatChecksum { get; }
		public string? Description { get; set; }

		public ICollection<TblAuthor> Authors { get; set; }

		public DateTimeOffset? CreationDate { get; set; }

		public DateTimeOffset? LastEditDate { get; set; }

		public DateTimeOffset? UploadDate { get; set; }
		[Browsable(false)]

		public ICollection<TblTag> Tags { get; set; }

		[Browsable(false)]
		public ICollection<TblModpack> Modpacks { get; set; }

		public ObjectAvailability Availability { get; set; }

		public TblLicence? Licence { get; set; }
	}
}
