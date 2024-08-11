using OpenLoco.ObjectEditor.Data;
using OpenLoco.ObjectEditor.Objects;

namespace AvaGui.Models
{
	public record IndexObjectHeader(string Name, DatFileType DatFileType, ObjectType ObjectType, SourceGame SourceGame, uint32_t Checksum, VehicleType? VehicleType);
}
