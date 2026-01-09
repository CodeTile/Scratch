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

			var cut = Render<DonutChart>(p => p
				.Add(x => x.Data, data)
				.Add(x => x.IncludeLabels, data.Keys)
			);

			Assert.AreEqual(2, cut.FindAll("path.donut-slice").Count);
		}

		[TestMethod]
		public void ClickingSlice_InvokesOnSliceClick()
		{
			string clicked = null;

			var data = new Dictionary<string, int> { ["North"] = 100 };

			var cut = Render<DonutChart>(p => p
				.Add(x => x.Data, data)
				.Add(x => x.IncludeLabels, data.Keys)
				.Add(x => x.OnSliceClick, label => clicked = label)
			);

			cut.Find("path.donut-slice").Click();

			Assert.AreEqual("North", clicked);
		}

		[TestMethod]
		public void ClickingCenter_InvokesOnCenterClick()
		{
			bool clicked = false;

			var data = new Dictionary<string, int> { ["A"] = 1 };

			var cut = Render<DonutChart>(p => p
				.Add(x => x.Data, data)
				.Add(x => x.IncludeLabels, data.Keys)
				.Add(x => x.IsDonut, true)
				.Add(x => x.OnCenterClick, () => clicked = true)
			);

			cut.Find("g.donut-center").Click();

			Assert.IsTrue(clicked);
		}

		[TestMethod]
		public void HoveringSlice_ShowsTooltip()
		{
			var data = new Dictionary<string, int> { ["North"] = 100 };

			var cut = Render<DonutChart>(p => p
				.Add(x => x.Data, data)
				.Add(x => x.IncludeLabels, data.Keys)
			);

			cut.Find("path.donut-slice").MouseOver();

			StringAssert.Contains(cut.Markup, "donut-tooltip");
		}

		[TestMethod]
		public void MouseLeave_HidesTooltip()
		{
			var data = new Dictionary<string, int> { ["North"] = 100 };

			var cut = Render<DonutChart>(p => p
				.Add(x => x.Data, data)
				.Add(x => x.IncludeLabels, data.Keys)
			);

			cut.Find("path.donut-slice").MouseOver();
			cut.Find("svg").MouseLeave();

			Assert.IsFalse(cut.Markup.Contains("donut-tooltip"));
		}

		[TestMethod]
		public void UpdatingData_ReRendersSlices()
		{
			var initial = new Dictionary<string, int> { ["A"] = 10 };
			var updated = new Dictionary<string, int> { ["A"] = 10, ["B"] = 20 };

			var cut = Render<DonutChart>(p => p
				.Add(x => x.Data, initial)
				.Add(x => x.IncludeLabels, initial.Keys)
			);

			cut = Render<DonutChart>(p => p
				.Add(x => x.Data, updated)
				.Add(x => x.IncludeLabels, updated.Keys)
			);

			Assert.AreEqual(2, cut.FindAll("path.donut-slice").Count);
		}
	}
}