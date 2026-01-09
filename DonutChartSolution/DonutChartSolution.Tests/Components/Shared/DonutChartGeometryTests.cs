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
			var cut = Render<DonutChart>(p => p
				.Add(x => x.IsDonut, true)
				.Add(x => x.Thickness, 30)
				.Add(x => x.Data, new Dictionary<string, int> { ["A"] = 100 })
			);

			Assert.AreEqual(60, cut.Instance.InnerRadius);
		}

		[TestMethod]
		public void PieMode_HasZeroInnerRadius()
		{
			var cut = Render<DonutChart>(p => p
				.Add(x => x.IsDonut, false)
				.Add(x => x.Data, new Dictionary<string, int> { ["A"] = 100 })
			);

			Assert.AreEqual(0, cut.Instance.InnerRadius);
		}

		[TestMethod]
		public void SvgContainsCorrectViewBox()
		{
			var cut = Render<DonutChart>(p => p
				.Add(x => x.Data, new Dictionary<string, int> { ["A"] = 100 })
			);

			StringAssert.Contains(cut.Markup, "viewBox=\"0 0 200 220\"");
		}

		[TestMethod]
		public void SlicesHaveCorrectCssClass()
		{
			var cut = Render<DonutChart>(p => p
				.Add(x => x.Data, new Dictionary<string, int> { ["A"] = 100 })
			);

			var cls = cut.Find("path.donut-slice").ClassList;

			Assert.IsTrue(cls.Contains("donut-slice"));
		}
	}
}