namespace PollyTest.Tests;

using System.Net;
using System.Net.Http;
using System.Text;

using Moq;

using Shouldly;

using Xunit;

public class BaseApiService_PostTests
{
	[Fact]
	public async Task BaseApiService_PostAsync_ShouldReturnCreatedObjectTest()
	{
		/// Arrange
		var json = """{"name":"created"}""";

		var response = new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent(json, Encoding.UTF8, "application/json")
		};

		var handler = new FakeHttpMessageHandler(response);

		var client = new HttpClient(handler)
		{
			BaseAddress = new Uri("https://test.com/")
		};

		var factory = new Mock<IHttpClientFactory>();
		factory.Setup(x => x.CreateClient("MyApi")).Returns(client);

		var uot = new TestApiService(factory.Object);

		var input = new CreateDto { Name = "input" };

		/// Act
		var result = await uot.PostAsyncTest("api/test", input);

		/// Assert
		result.ShouldNotBeNull();
		result!.Name.ShouldBe("created");
	}
}
