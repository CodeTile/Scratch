using System.Collections.Generic;

using Bunit;

using DonutChartSolution.Components.Shared;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MudBlazor.Services;

using Shouldly;

namespace DonutChartSolution.Tests
{
	/// <summary>
	/// Comprehensive test suite for the DonutChart component.
	/// Includes rendering, data, navigation, JSInterop, snapshot, and CSS tests.
	/// </summary>
	[TestClass]
	public class DonutChartTests
	{
		private Bunit.TestContext _ctx = null!;

		/// <summary>
		/// Initializes a fresh bUnit test context before each test.
		/// </summary>
		[TestInitialize]
		public void Setup()
		{
			_ctx = new Bunit.TestContext();
			_ctx.Services.AddMudServices();
			_ctx.Services.AddSingleton<NavigationManager, TestNavigationManager>();
		}

		/// <summary>
		/// Disposes the bUnit test context after each test.
		/// </summary>
		[TestCleanup]
		public void TearDown()
		{
			_ctx.Dispose();
		}

		// --------------------------------------------------------------------
		// Rendering Tests
		// --------------------------------------------------------------------

		/// <summary>
		/// Ensures the title renders correctly.
		/// </summary>
		[TestMethod]
		[TestCategory("Rendering")]
		public void DonutChart_RendersTitle()
		{
			var cut = _ctx.Render<DonutChart>(p => p
				.Add(x => x.Title, "Energy Chart")
				.Add(x => x.Data, new[]
				{
					new KeyValuePair<string,int>("Gas",100)
				})
			);

			cut.Markup.ShouldContain("Energy Chart");
		}

		/// <summary>
		/// Snapshot test for initial rendering.
		/// </summary>
		[TestMethod]
		[TestCategory("Snapshot")]
		public void DonutChart_InitialRender_Snapshot()
		{
			var cut = _ctx.Render<DonutChart>(p => p
				.Add(x => x.Title, "Snapshot Chart")
				.Add(x => x.Data, new[]
				{
					new KeyValuePair<string,int>("Gas",100),
					new KeyValuePair<string,int>("Wind",200)
				})
			);

			cut.MarkupMatches(@"
<div class=""donut-chart-wrapper"">
  <mudpaper class=""donut-chart-container"">
    <div class=""donut-title"">Snapshot Chart</div>
  </mudpaper>
</div>");
		}

		// --------------------------------------------------------------------
		// Data Tests
		// --------------------------------------------------------------------

		/// <summary>
		/// Ensures total value is calculated correctly.
		/// </summary>
		[TestMethod]
		[TestCategory("Data")]
		public void DonutChart_ShowsCorrectTotal()
		{
			var data = new List<KeyValuePair<string, int>>
			{
				new("Gas", 100),
				new("Wind", 200)
			};

			var cut = _ctx.Render<DonutChart>(p => p.Add(x => x.Data, data));

			cut.Markup.ShouldContain("300");
		}

		/// <summary>
		/// Ensures dictionary input is accepted.
		/// </summary>
		[TestMethod]
		[TestCategory("Data")]
		public void DonutChart_AcceptsDictionary()
		{
			var dict = new Dictionary<string, int>
			{
				{ "Solar", 250 },
				{ "BioMass", 10 }
			};

			var cut = _ctx.Render<DonutChart>(p => p.Add(x => x.DataDictionary, dict));

			cut.Markup.ShouldContain("Solar");
			cut.Markup.ShouldContain("BioMass");
		}

		// --------------------------------------------------------------------
		// Navigation Tests
		// --------------------------------------------------------------------

		/// <summary>
		/// Clicking the center navigates to /weather.
		/// </summary>
		[TestMethod]
		[TestCategory("Navigation")]
		public void DonutChart_CenterClick_NavigatesToWeather()
		{
			var nav = (TestNavigationManager)_ctx.Services.GetRequiredService<NavigationManager>();

			_ctx.JSInterop.SetupVoid("donutChart.registerSliceClicks", _ => true);

			var cut = _ctx.Render<DonutChart>(p => p
				.Add(x => x.Data, new[]
				{
					new KeyValuePair<string,int>("Gas",100)
				})
			);

			cut.Find(".donut-center-group").Click();

			nav.Uri.Replace(nav.BaseUri, "").ShouldBe("/weather");
		}

		/// <summary>
		/// JS callback for slice click navigates to /counter.
		/// </summary>
		[TestMethod]
		[TestCategory("Navigation")]
		[TestCategory("JSInterop")]
		public void DonutChart_SliceClick_NavigatesToCounter()
		{
			var nav = (TestNavigationManager)_ctx.Services.GetRequiredService<NavigationManager>();

			_ctx.JSInterop.SetupVoid("donutChart.registerSliceClicks", _ => true);

			var cut = _ctx.Render<DonutChart>(p => p
				.Add(x => x.Data, new[]
				{
					new KeyValuePair<string,int>("Gas",100),
					new KeyValuePair<string,int>("Wind",200)
				})
			);

			cut.Instance.SliceClicked(0);

			nav.Uri.Replace(nav.BaseUri, "").ShouldBe("/counter");
		}

		// --------------------------------------------------------------------
		// CSS Tests
		// --------------------------------------------------------------------

		/// <summary>
		/// Ensures required CSS classes are present in the markup.
		/// </summary>
		[TestMethod]
		[TestCategory("CSS")]
		public void DonutChart_HasRequiredCssClasses()
		{
			_ctx.JSInterop.SetupVoid("donutChart.registerSliceClicks", _ => true);

			var cut = _ctx.Render<DonutChart>(p => p
				.Add(x => x.Data, new[]
				{
					new KeyValuePair<string,int>("Gas",100)
				})
			);

			var html = cut.Markup;

			html.ShouldContain("donut-chart-wrapper");
			html.ShouldContain("donut-chart-container");
			html.ShouldContain("donut-title");
			html.ShouldContain("donut-center-group");
			html.ShouldContain("donut-inner-title");
			html.ShouldContain("donut-inner-text");
		}
	}

	/// <summary>
	/// Test NavigationManager for verifying navigation behavior.
	/// </summary>
	public class TestNavigationManager : NavigationManager
	{
		public TestNavigationManager()
		{
			Initialize("http://localhost/", "http://localhost/");
		}

		protected override void NavigateToCore(string uri, bool forceLoad)
		{
			Uri = ToAbsoluteUri(uri).ToString();
		}
	}
}
