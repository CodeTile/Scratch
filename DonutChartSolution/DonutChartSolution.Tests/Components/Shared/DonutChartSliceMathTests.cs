using Bunit;

using DonutChartSolution.Components.Shared;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DonutChartSolution.Tests.Components.Shared
{
	[TestClass]
	public class DonutChartSliceMathTests : BunitContext
	{
		[TestMethod]
		public void ZeroValueSlices_AreIgnored()
		{
			var cut = Render<DonutChart>(p => p
				.Add(x => x.Data, new Dictionary<string, int>
				{
					["A"] = 0,
					["B"] = 50
				})
			);

			Assert.AreEqual(1, cut.FindAll("path.donut-slice").Count);
		}

		[TestMethod]
		public void AllZeroValues_ProduceNoSlices()
		{
			var cut = Render<DonutChart>(p => p
				.Add(x => x.Data, new Dictionary<string, int>
				{
					["A"] = 0,
					["B"] = 0
				})
			);

			Assert.AreEqual(0, cut.FindAll("path.donut-slice").Count);
		}

		[TestMethod]
		public void SingleSlice_FillsFullCircle()
		{
			var cut = Render<DonutChart>(p => p
				.Add(x => x.Data, new Dictionary<string, int> { ["A"] = 100 })
			);

			var path = cut.Find("path.donut-slice").GetAttribute("d");

			StringAssert.Contains(path, "A 90 90 0 1 1");
		}

		[TestMethod]
		public void LargeValues_StillProduceValidAngles()
		{
			var cut = Render<DonutChart>(p => p
				.Add(x => x.Data, new Dictionary<string, int>
				{
					["A"] = 1_000_000,
					["B"] = 500_000
				})
			);

			Assert.AreEqual(2, cut.FindAll("path.donut-slice").Count);
		}
	}
}