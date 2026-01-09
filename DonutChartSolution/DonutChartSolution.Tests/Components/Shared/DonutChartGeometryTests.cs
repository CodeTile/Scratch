using Bunit;

using DonutChartSolution.Components.Shared;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DonutChartSolution.Tests.Components.Shared
{
	[TestClass]
	public class DonutChartGeometryTests : BunitContext
	{
		[TestMethod]
		public void DonutMode_UsesInnerRadius()
		{
			var data = new Dictionary<string, int> { ["A"] = 100 };

			var cut = Render<DonutChart>(p => p
				.Add(x => x.Data, data)
				.Add(x => x.IncludeLabels, data.Keys)
				.Add(x => x.IsDonut, true)
				.Add(x => x.Thickness, 30)
			);

			Assert.AreEqual(60, cut.Instance.InnerRadius);
		}

		[TestMethod]
		public void PieMode_HasZeroInnerRadius()
		{
			var data = new Dictionary<string, int> { ["A"] = 100 };

			var cut = Render<DonutChart>(p => p
				.Add(x => x.Data, data)
				.Add(x => x.IncludeLabels, data.Keys)
				.Add(x => x.IsDonut, false)
			);

			Assert.AreEqual(0, cut.Instance.InnerRadius);
		}

		[TestMethod]
		public void SvgContainsCorrectViewBox()
		{
			var data = new Dictionary<string, int> { ["A"] = 100 };

			var cut = Render<DonutChart>(p => p
				.Add(x => x.Data, data)
				.Add(x => x.IncludeLabels, data.Keys)
			);

			StringAssert.Contains(cut.Markup, "viewBox=\"0 0 200 220\"");
		}

		[TestMethod]
		public void SlicesHaveCorrectCssClass()
		{
			var data = new Dictionary<string, int> { ["A"] = 100 };

			var cut = Render<DonutChart>(p => p
				.Add(x => x.Data, data)
				.Add(x => x.IncludeLabels, data.Keys)
			);

			var cls = cut.Find("path.donut-slice").ClassList;

			Assert.IsTrue(cls.Contains("donut-slice"));
		}
	}
}