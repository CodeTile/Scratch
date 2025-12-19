using System.Collections.Generic;

using Bunit;

using DonutChartSolution.Components.Shared;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DonutChartTests
{
	[TestClass]
	public class DonutChartTests
	{
		private Bunit.TestContext testContext;

		[TestInitialize]
		public void Setup() => testContext = new Bunit.TestContext();

		[TestCleanup]
		public void Cleanup() => testContext.Dispose();

		[TestMethod]
		public void DonutChart_RenderedCorrectly()
		{
			// Arrange
			var data = new List<KeyValuePair<string, int>>
			{
				new KeyValuePair<string,int>("Gas",100),
				new KeyValuePair<string,int>("Wind",200)
			};

			// Act
			var cut = testContext.Render<DonutChart>(parameters => parameters
															.Add(p => p.Title, "Energy Mix")
															.Add(p => p.Data, data)
															.Add(p => p.InnerLabel, "Total")
														);


			// Assert
			cut.Markup.Contains("Energy Mix");
			cut.Markup.Contains("Total");
		}
	}
}
