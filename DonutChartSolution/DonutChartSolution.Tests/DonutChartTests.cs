using System.Collections.Generic;
using System.Linq;

using Bunit;

using DonutChartSolution.Components.Shared;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MudBlazor.Services;

namespace DonutChartSolution.Tests
{
	[TestClass]
	public class DonutChartTests
	{
		private Bunit.TestContext _ctx = null!;

		[TestInitialize]
		public void Setup()
		{
			_ctx = new Bunit.TestContext();

			_ctx.Services.AddMudServices();
			_ctx.Services.AddSingleton<NavigationManager, TestNavigationManager>();
		}

		[TestCleanup]
		public void TearDown()
		{
			_ctx.Dispose();
		}

		// ---------------------------------------------------------
		// BASIC RENDERING
		// ---------------------------------------------------------
		[TestMethod]
		public void DonutChart_RendersTitle()
		{
			var cut = _ctx.Render<DonutChart>(p => p
				.Add(x => x.Title, "Energy Chart")
				.Add(x => x.Data, new[]
				{
			new KeyValuePair<string,int>("Gas",100)
				})
			);

			StringAssert.Contains(cut.Markup, "Energy Chart");
		}


		// ---------------------------------------------------------
		// TOTAL VALUE
		// ---------------------------------------------------------
		[TestMethod]
		public void DonutChart_ShowsCorrectTotal()
		{
			var data = new List<KeyValuePair<string, int>>
			{
				new("Gas", 100),
				new("Wind", 200)
			};

			var cut = _ctx.Render<DonutChart>(p => p
				.Add(x => x.Data, data)
			);

			StringAssert.Contains(cut.Markup, "300");
		}

		// ---------------------------------------------------------
		// DICTIONARY INPUT
		// ---------------------------------------------------------
		[TestMethod]
		public void DonutChart_AcceptsDictionary()
		{
			var dict = new Dictionary<string, int>
			{
				{ "Solar", 250 },
				{ "BioMass", 10 }
			};

			var cut = _ctx.Render<DonutChart>(p => p
				.Add(x => x.DataDictionary, dict)
			);

			StringAssert.Contains(cut.Markup, "Solar");
			StringAssert.Contains(cut.Markup, "BioMass");
		}

		// ---------------------------------------------------------
		// CENTER CLICK NAVIGATION
		// ---------------------------------------------------------
		[TestMethod]
		public void DonutChart_CenterClick_NavigatesToWeather()
		{
			var nav = (TestNavigationManager)_ctx.Services.GetRequiredService<NavigationManager>();

			var cut = _ctx.Render<DonutChart>(p => p
				.Add(x => x.Data, new[]
				{
					new KeyValuePair<string,int>("Gas",100)
				})
			);

			var center = cut.Find(".donut-center-group");
			center.Click();

			Assert.AreEqual("/weather", nav.Uri.Replace(nav.BaseUri, ""));
		}

		// ---------------------------------------------------------
		// SLICE CLICK VIA JS INVOCATION
		// ---------------------------------------------------------
		[TestMethod]
		public void DonutChart_SliceClick_NavigatesToCounter()
		{
			var nav = (TestNavigationManager)_ctx.Services.GetRequiredService<NavigationManager>();

			var cut = _ctx.Render<DonutChart>(p => p
				.Add(x => x.Data, new[]
				{
					new KeyValuePair<string,int>("Gas",100),
					new KeyValuePair<string,int>("Wind",200)
				})
			);

			cut.Instance.SliceClicked(0);

			Assert.AreEqual("/counter", nav.Uri.Replace(nav.BaseUri, ""));
		}
	}

	// ---------------------------------------------------------
	// TEST NAVIGATION MANAGER
	// ---------------------------------------------------------
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
