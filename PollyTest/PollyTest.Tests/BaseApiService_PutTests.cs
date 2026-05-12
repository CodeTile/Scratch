namespace PollyTest.Tests;

using System.Net;
using System.Net.Http;

using Moq;

using Shouldly;

using Xunit;

public class BaseApiService_PutTests
{
	[Fact]
	public async Task BaseApiService_PutAsync_ShouldSendRequest_SuccessTest()
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

		var input = new UpdateDto { Name = "updated" };

		/// Act
		await uot.PutAsyncTest("api/test/1", input);

		/// Assert
		true.ShouldBeTrue(); // success = no exception
	}
}
