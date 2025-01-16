using NUnit.Framework;
using NUnit.Framework.Internal;
using OpenLoco.Definitions.Web;
using Logger = OpenLoco.Common.Logging.Logger;

namespace OpenLoco.Dat.Tests
{
	[TestFixture, Explicit("This is a production test for the live object service. Do not run this as part of the regular unit test suite. Only run this test manually when deploying new versions of the object service.")]
	public class LiveServiceTests
	{
		HttpClient WebClient { get; set; }
		Logger DummyLogger { get; set; }

		[SetUp]
		public void Init()
		{
			Assert.That(Uri.TryCreate("https://openloco.leftofzen.dev", new(), out var liveServiceAddress), Is.True);
			WebClient = new HttpClient() { BaseAddress = liveServiceAddress, };
			DummyLogger = new Logger();

			// this is to give a 1s break between tests to not DoS the service
			Thread.Sleep(1000);
		}

		[TearDown]
		public void Cleanup()
		{
			WebClient.Dispose();
		}

		[Test]
		public async Task Objects_List()
		{
			var result = await Client.Get.ObjectListAsync(WebClient);
			Assert.That(result, Is.Not.Null);
			Assert.That(result.Count(), Is.GreaterThan(31000));
		}

		[Test]
		public async Task Objects_GetDat()
		{
			var result = await Client.Get.DatAsync(WebClient, "", 0, false);
			Assert.That(result, Is.Not.Null);
		}

		[Test]
		public async Task Objects_GetDatFile()
		{
			var result = await Client.Get.DatFileAsync(WebClient, "", 0);
			Assert.That(result, Is.Not.Null);
		}

		[Test]
		public async Task Objects_Objects_GetObject()
		{
			var result = await Client.Get.ObjectAsync(WebClient, 0, false);
			Assert.That(result, Is.Not.Null);
		}

		[Test]
		public async Task Objects_GetObjectFile()
		{
			var result = await Client.Get.ObjectFileAsync(WebClient, 0);
			Assert.That(result, Is.Not.Null);
		}

		[Test]
		public async Task Objects_GetObjectImages()
		{
			var result = await Client.Get.ObjectImagesAsync(WebClient, 0);
			Assert.That(result, Is.Not.Null);
		}

		[Test]
		public async Task Scenarios_List()
		{
			var result = await Client.Get.ScenarioListAsync(WebClient);
			Assert.That(result, Is.Not.Null);
		}

		[Test]
		public async Task Scenarios_GetScenario()
		{
			var result = await Client.Get.ScenarioAsync(WebClient, "", 0, false);
			Assert.That(result, Is.Not.Null);
		}

		[Test]
		public async Task Objects_UploadDat()
		{
			// this request should intentionally fail so we don't upload garbage to the server. the failure mechanism is an empty byte[].
			var result = await Client.Post.UploadDatFileAsync(WebClient, "FAKE-FILE", [], DateTimeOffset.Now, DummyLogger);
			Assert.That(result, Is.False);
		}

		[Test]
		public void Objects_UploadObject()
			=> _ = Assert.Throws<NotImplementedException>(async () => await Client.Post.UploadObjectFileAsync(WebClient, "", [], DateTimeOffset.Now, DummyLogger));
	}
}
