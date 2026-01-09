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
			var data = new Dictionary<string, int>
			{
				["A"] = 0,
				["B"] = 50
			};

			var cut = Render<DonutChart>(p => p
				.Add(x => x.Data, data)
				.Add(x => x.IncludeLabels, data.Keys)
			);

			Assert.AreEqual(1, cut.FindAll("path.donut-slice").Count);
		}

		[TestMethod]
		public void AllZeroValues_ProduceNoSlices()
		{
			var data = new Dictionary<string, int>
			{
				["A"] = 0,
				["B"] = 0
			};

			var cut = Render<DonutChart>(p => p
				.Add(x => x.Data, data)
				.Add(x => x.IncludeLabels, data.Keys)
			);

			Assert.AreEqual(0, cut.FindAll("path.donut-slice").Count);
		}

		[TestMethod]
		public void SingleSlice_FillsFullCircle()
		{
			var data = new Dictionary<string, int> { ["A"] = 100 };

			var cut = Render<DonutChart>(p => p
				.Add(x => x.Data, data)
				.Add(x => x.IncludeLabels, data.Keys)
			);

			var path = cut.Find("path.donut-slice").GetAttribute("d");

			StringAssert.Contains(path, "A 90 90 0 1 1");
		}

		[TestMethod]
		public void LargeValues_StillProduceValidAngles()
		{
			var data = new Dictionary<string, int>
			{
				["A"] = 1_000_000,
				["B"] = 500_000
			};

			var cut = Render<DonutChart>(p => p
				.Add(x => x.Data, data)
				.Add(x => x.IncludeLabels, data.Keys)
			);

			Assert.AreEqual(2, cut.FindAll("path.donut-slice").Count);
		}
	}
}