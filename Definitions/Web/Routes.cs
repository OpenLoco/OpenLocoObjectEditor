namespace OpenLoco.Definitions.Web
{
	public static class Routes
	{
		// GET
		public const string ListObjects = "/objects/list";
		public const string GetObject = "/objects/{id:int}/object";
		public const string GetObjectFile = "/objects/{id:int}/file";
		public const string GetObjectImages = "/objects/{id:int}/images";

		public const string GetDat = "/objects/dat";
		public const string GetDatFile = "/objects/datfile";

		public const string ListObjectPacks = "/objectpacks/list";
		public const string GetObjectPack = "/objectpacks/{id:int}";

		public const string ListScenarios = "/scenarios/list";
		public const string GetScenario = "/scenarios/{id:int}";

		public const string ListSC5FilePacks = "/sc5filepacks/list";
		public const string GetSC5FilePack = "/sc5filepacks/{id:int}";

		public const string ListAuthors = "/authors/list";
		public const string ListLicences = "/licences/list";
		public const string ListTags = "/tags/list";

		// POST
		public const string UploadDat = "/objects/uploaddat";
		public const string UploadObject = "/objects/uploadobject";

		// PATCH
		public const string UpdateDat = "/objects/{id:int}/dat";
		public const string UpdateObject = "/objects/{id:int}/object";
	}
}
