namespace PollyTest.Tests;

using System.Net;
using System.Net.Http;

using Moq;

using Shouldly;

using Xunit;

public class BaseApiService_DeleteTests
{
	[Fact]
	public async Task BaseApiService_DeleteAsync_ShouldCompleteSuccessfullyTest()
	{
		/// Arrange
		var response = new HttpResponseMessage(HttpStatusCode.NoContent);

		var handler = new FakeHttpMessageHandler(response);

		var client = new HttpClient(handler)
		{
			BaseAddress = new Uri("https://test.com/")
		};

		var factory = new Mock<IHttpClientFactory>();
		factory.Setup(x => x.CreateClient("MyApi")).Returns(client);

		var uot = new TestApiService(factory.Object);

		/// Act
		await uot.DeleteAsyncTest("api/test/1");

		/// Assert
		true.ShouldBeTrue(); // no exception = success
	}
}