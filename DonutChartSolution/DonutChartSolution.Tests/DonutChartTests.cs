using System.Collections.Generic;

using Bunit;
using Bunit.TestDoubles;

using DonutChartSolution.Components.Shared;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DonutChartSolution.Tests
{
	[TestClass]
	public class DonutChartTests
	{
		private Bunit.TestContext ctx;

		[TestInitialize]
		public void Setup() => ctx = new Bunit.TestContext();

		[TestCleanup]
		public void Cleanup() => ctx.Dispose();

		[TestMethod]
		public void DonutChart_RendersTitleAndTotal()
		{
			// Arrange
			var data = new List<KeyValuePair<string, int>>
			{
				new("Gas", 100),
				new("Wind", 200)
			};

			// Act
			var cut = ctx.Render<DonutChart>(parameters => parameters
				.Add(p => p.Title, "Energy Mix")
				.Add(p => p.Data, data)
				.Add(p => p.InnerLabel, "Total")
			);

			// Assert
			Assert.IsTrue(cut.Markup.Contains("Energy Mix"));
			Assert.IsTrue(cut.Markup.Contains("Total"));
			Assert.IsTrue(cut.Markup.Contains("300")); // sum of 100+200
		}

		[TestMethod]
		public void DonutChart_CenterClick_NavigatesToWeather()
		{
			// Arrange
			var data = new List<KeyValuePair<string, int>> { new("Gas", 100) };

			var navMan = ctx.Services.GetService<BunitNavigationManager>();
			Assert.IsNotNull(navMan);

			var cut = ctx.Render<DonutChart>(parameters => parameters
				.Add(p => p.Data, data)
			);

			// Act: click the center text
			cut.Find(".donut-inner-text").Click();

			// Assert navigation
			Assert.AreEqual("weather", navMan!.Uri.Replace(navMan.BaseUri, ""));
		}

		[TestMethod]
		public void DonutChart_SliceClick_NavigatesToCounter()
		{
			// Arrange
			var data = new List<KeyValuePair<string, int>>
			{
				new("Gas", 100),
				new("Wind", 200)
			};

			var navMan = ctx.Services.GetService<BunitNavigationManager>();
			Assert.IsNotNull(navMan);

			var cut = ctx.Render<DonutChart>(parameters => parameters
				.Add(p => p.Data, data)
			);

			// Act: simulate slice click by setting SelectedIndex and triggering OnParametersSet
			cut.Instance.SelectedIndex = 0;
			cut.Render(); // triggers OnParametersSet

			// Assert navigation
			Assert.AreEqual("counter", navMan!.Uri.Replace(navMan.BaseUri, ""));
		}
	}
}
