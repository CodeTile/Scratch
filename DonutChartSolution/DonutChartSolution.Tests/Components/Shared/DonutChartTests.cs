using Bunit;

using DonutChartSolution.Components.Shared;

using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Shouldly;

namespace DonutChartSolution.Tests.Components.Shared
{
	[TestClass]
	public class DonutChartTests
	{
		private Bunit.TestContext _bunit = null!;

		[TestInitialize]
		public void Setup()
		{
			_bunit = new Bunit.TestContext();
		}

		[TestCleanup]
		public void Cleanup()
		{
			_bunit.Dispose();
		}

		// ---------------------------------------------------------
		// Rendering Tests
		// ---------------------------------------------------------

		[TestMethod]
		public void DonutChart_RendersSvg()
		{
			var cut = _bunit.Render<DonutChart>(p => p
				.Add(x => x.Data, new Dictionary<string, int> { ["A"] = 10 })
			);

			cut.Find("svg").ShouldNotBeNull();
		}

		[TestMethod]
		public void DonutChart_RendersCorrectNumberOfSlices()
		{
			var data = new Dictionary<string, int>
			{
				["A"] = 10,
				["B"] = 20,
				["C"] = 30
			};

			var cut = _bunit.Render<DonutChart>(p => p
				.Add(x => x.Data, data)
			);

			var slices = cut.FindAll("path.donut-slice");
			slices.Count.ShouldBe(3);
		}

		// ---------------------------------------------------------
		// Data Normalization
		// ---------------------------------------------------------

		[TestMethod]
		public void DonutChart_UsesDataParameter()
		{
			var cut = _bunit.Render<DonutChart>(p => p
				.Add(x => x.Data, new Dictionary<string, int> { ["X"] = 50 })
			);

			cut.Find("path.donut-slice").ShouldNotBeNull();
		}

		[TestMethod]
		public void DonutChart_UsesItemsParameter_WhenDataNull()
		{
			var items = new List<KeyValuePair<string, int>>
			{
				new("Item1", 10),
				new("Item2", 20)
			};

			var cut = _bunit.Render<DonutChart>(p => p
				.Add(x => x.Items, items)
			);

			cut.FindAll("path.donut-slice").Count.ShouldBe(2);
		}

		// ---------------------------------------------------------
		// Geometry Tests
		// ---------------------------------------------------------

		[TestMethod]
		public void DonutChart_ComputesTotalValue()
		{
			var data = new Dictionary<string, int>
			{
				["A"] = 10,
				["B"] = 20
			};

			var cut = _bunit.Render<DonutChart>(p => p
				.Add(x => x.Data, data)
			);

			var instance = cut.Instance;
			var total = (int)instance.GetType()
				.GetProperty("TotalValue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
				.GetValue(instance)!;

			total.ShouldBe(30);
		}

		[TestMethod]
		public void DonutChart_InnerRadiusZero_WhenPie()
		{
			var cut = _bunit.Render<DonutChart>(p => p
				.Add(x => x.IsDonut, false)
				.Add(x => x.Data, new Dictionary<string, int> { ["A"] = 10 })
			);

			var instance = cut.Instance;
			var inner = (double)instance.GetType()
				.GetProperty("InnerRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
				.GetValue(instance)!;

			inner.ShouldBe(0d);
		}

		// ---------------------------------------------------------
		// Event Callback Tests
		// ---------------------------------------------------------

		[TestMethod]
		public void DonutChart_SliceClick_InvokesCallback()
		{
			string? clicked = null;

			var cut = _bunit.Render<DonutChart>(p => p
				.Add(x => x.Data, new Dictionary<string, int> { ["A"] = 10 })
				.Add(x => x.OnSliceSelected, (string label) => clicked = label)
			);

			cut.Find("path.donut-slice").Click();

			clicked.ShouldBe("A");
		}

		[TestMethod]
		public void DonutChart_CenterClick_InvokesCallback_WhenDonut()
		{
			bool clicked = false;

			var cut = _bunit.Render<DonutChart>(p => p
				.Add(x => x.IsDonut, true)
				.Add(x => x.Data, new Dictionary<string, int> { ["A"] = 10 })
				.Add(x => x.OnCenterSelected, EventCallback.Factory.Create(this, () => clicked = true))
			);

			cut.Find("g.donut-center").Click();

			clicked.ShouldBeTrue();
		}

		[TestMethod]
		public void DonutChart_CenterClick_Ignored_WhenPie()
		{
			bool clicked = false;

			var cut = _bunit.Render<DonutChart>(p => p
				.Add(x => x.IsDonut, false)
				.Add(x => x.Data, new Dictionary<string, int> { ["A"] = 10 })
				.Add(x => x.OnCenterSelected, EventCallback.Factory.Create(this, () => clicked = true))
			);

			Should.Throw<ElementNotFoundException>(() => cut.Find("g.donut-center"));
			clicked.ShouldBeFalse();
		}

		// ---------------------------------------------------------
		// Tooltip Tests
		// ---------------------------------------------------------

		[TestMethod]
		public void DonutChart_HoverSlice_ShowsTooltip()
		{
			var cut = _bunit.Render<DonutChart>(p => p
				.Add(x => x.Data, new Dictionary<string, int> { ["A"] = 10 })
			);

			cut.Find("path.donut-slice").MouseOver();

			cut.Find(".donut-tooltip").ShouldNotBeNull();
		}

		[TestMethod]
		public void DonutChart_MouseLeave_HidesTooltip()
		{
			var cut = _bunit.Render<DonutChart>(p => p
				.Add(x => x.Data, new Dictionary<string, int> { ["A"] = 10 })
			);

			cut.Find("path.donut-slice").MouseOver();
			cut.Find("svg").MouseLeave();

			Should.Throw<ElementNotFoundException>(() => cut.Find(".donut-tooltip"));
		}
	}
}
