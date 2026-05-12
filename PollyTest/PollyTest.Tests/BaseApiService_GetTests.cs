namespace PollyTest.Tests;

using System.Net;
using System.Net.Http;
using System.Text;

using Moq;

using Shouldly;

using Xunit;

public class BaseApiService_GetTests
{
	[Fact]
	public async Task BaseApiService_GetAsync_ShouldReturnDeserializedObjectTest()
	{
		/// Arrange
		var json = """{"name":"test"}""";

		var response = new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent(json, Encoding.UTF8, "application/json")
		};

		var handler = new FakeHttpMessageHandler(response);
		var httpClient = new HttpClient(handler)
		{
			BaseAddress = new Uri("https://test.com/")
		};

		var factory = new Mock<IHttpClientFactory>();

		factory
			.Setup(x => x.CreateClient("MyApi"))
			.Returns(httpClient);

		var uot = new TestApiService(factory.Object);

		/// Act
		var result = await uot.GetAsyncTest("api/test");

		/// Assert
		result.ShouldNotBeNull();
		result!.Name.ShouldBe("test");
	}

	[Fact]
	public async Task BaseApiService_GetAsync_ShouldThrow_WhenStatusIsNotSuccessTest()
	{
		/// Arrange
		var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);

		var handler = new FakeHttpMessageHandler(response);

		var httpClient = new HttpClient(handler)
		{
			BaseAddress = new Uri("https://test.com/")
		};

		var factory = new Mock<IHttpClientFactory>();

		factory
			.Setup(x => x.CreateClient("MyApi"))
			.Returns(httpClient);

		var uot = new TestApiService(factory.Object);

		/// Act & Assert
		await Should.ThrowAsync<HttpRequestException>(async () =>
		{
			await uot.GetAsyncTest("api/test");
		});
	}
}
