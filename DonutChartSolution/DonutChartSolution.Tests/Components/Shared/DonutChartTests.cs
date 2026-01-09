using Bunit;

using DonutChartSolution.Components.Shared;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DonutChartSolution.Tests.Components.Shared
{
	[TestClass]
	public class DonutChartTests : BunitContext
	{
		[TestMethod]
		public void RendersCorrectNumberOfSlices()
		{
			var data = new Dictionary<string, int>
			{
				["North"] = 100,
				["South"] = 50
			};

			var cut = Render<DonutChart>(p => p.Add(x => x.Data, data));

			Assert.AreEqual(2, cut.FindAll("path.donut-slice").Count);
		}

		[TestMethod]
		public void ClickingSlice_InvokesOnSliceClick()
		{
			string clicked = null;

			var cut = Render<DonutChart>(p => p
				.Add(x => x.Data, new Dictionary<string, int> { ["North"] = 100 })
				.Add(x => x.OnSliceClick, label => clicked = label)
			);

			cut.Find("path.donut-slice").Click();

			Assert.AreEqual("North", clicked);
		}

		[TestMethod]
		public void ClickingCenter_InvokesOnCenterClick()
		{
			bool clicked = false;

			var cut = Render<DonutChart>(p => p
				.Add(x => x.Data, new Dictionary<string, int> { ["A"] = 1 })
				.Add(x => x.IsDonut, true)
				.Add(x => x.OnCenterClick, () => clicked = true)
			);

			cut.Find("g.donut-center").Click();

			Assert.IsTrue(clicked);
		}

		[TestMethod]
		public void HoveringSlice_ShowsTooltip()
		{
			var cut = Render<DonutChart>(p => p
				.Add(x => x.Data, new Dictionary<string, int> { ["North"] = 100 })
			);

			cut.Find("path.donut-slice").MouseOver();

			StringAssert.Contains(cut.Markup, "donut-tooltip");
		}

		[TestMethod]
		public void MouseLeave_HidesTooltip()
		{
			var cut = Render<DonutChart>(p => p
				.Add(x => x.Data, new Dictionary<string, int> { ["North"] = 100 })
			);

			cut.Find("path.donut-slice").MouseOver();
			cut.Find("svg").MouseLeave();

			StringAssert.DoesNotMatch(cut.Markup, new System.Text.RegularExpressions.Regex("donut-tooltip"));
		}

		[TestMethod]
		public void UpdatingData_ReRendersSlices()
		{
			var cut = Render<DonutChart>(p => p
				.Add(x => x.Data, new Dictionary<string, int> { ["A"] = 10 })
			);

			cut = Render<DonutChart>(p => p
				.Add(x => x.Data, new Dictionary<string, int> { ["A"] = 10, ["B"] = 20 })
			);

			Assert.AreEqual(2, cut.FindAll("path.donut-slice").Count);
		}
	}
}