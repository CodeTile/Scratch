using System.Collections;
using System.Reflection;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using DonutChartSolution.Components.Shared;

namespace DonutChartSolution.Tests.Components.Shared
{
	[TestClass]
	public class DonutChartSliceMathTests
	{
		private Bunit.BunitContext _bunit = null!;

		[TestInitialize]
		public void Setup()
		{
			_bunit = new Bunit.BunitContext();
		}

		[TestCleanup]
		public void Cleanup()
		{
			_bunit.Dispose();
		}

		// ---------------------------------------------------------
		// Reflection helpers
		// ---------------------------------------------------------

		private static IList GetSlices(DonutChart instance)
		{
			var field = typeof(DonutChart)
				.GetField("Slices", BindingFlags.NonPublic | BindingFlags.Instance)
				?? throw new InvalidOperationException("Slices field not found");

			return (IList)field.GetValue(instance)!;
		}

		private static double GetSliceDouble(object slice, string property)
		{
			var prop = slice.GetType().GetProperty(property)
					   ?? throw new InvalidOperationException($"{property} not found");

			return Convert.ToDouble(prop.GetValue(slice)!);
		}

		private static string GetSliceLabel(object slice)
		{
			var prop = slice.GetType().GetProperty("Label")
					   ?? throw new InvalidOperationException("Label not found");

			return (string)prop.GetValue(slice)!;
		}

		// ---------------------------------------------------------
		// Tests
		// ---------------------------------------------------------

		[TestMethod]
		public void SweepAngles_SumTo360()
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

			var instance = cut.Instance;
			var slices = GetSlices(instance);

			var totalSweep = slices.Cast<object>()
				.Sum(s => GetSliceDouble(s, "SweepAngle"));

			totalSweep.ShouldBe(360.0, 1e-6);
		}

		[TestMethod]
		public void StartAngles_ProgressCorrectly()
		{
			var data = new Dictionary<string, int>
			{
				["A"] = 25,
				["B"] = 25,
				["C"] = 50
			};

			var cut = _bunit.Render<DonutChart>(p => p
				.Add(x => x.Data, data)
			);

			var instance = cut.Instance;
			var slices = GetSlices(instance);

			slices.Count.ShouldBe(3);

			var start0 = GetSliceDouble(slices[0], "StartAngle");
			var sweep0 = GetSliceDouble(slices[0], "SweepAngle");

			var start1 = GetSliceDouble(slices[1], "StartAngle");
			var sweep1 = GetSliceDouble(slices[1], "SweepAngle");

			var start2 = GetSliceDouble(slices[2], "StartAngle");

			start1.ShouldBe(start0 + sweep0, 1e-6);
			start2.ShouldBe(start1 + sweep1, 1e-6);
		}

		[TestMethod]
		public void ZeroOrNegativeValues_AreIgnored()
		{
			var data = new Dictionary<string, int>
			{
				["A"] = 10,
				["B"] = 0,
				["C"] = -5,
				["D"] = 5
			};

			var cut = _bunit.Render<DonutChart>(p => p
				.Add(x => x.Data, data)
			);

			var instance = cut.Instance;
			var slices = GetSlices(instance);

			slices.Count.ShouldBe(2);

			var labels = slices.Cast<object>()
				.Select(GetSliceLabel)
				.ToList();

			labels.ShouldContain("A");
			labels.ShouldContain("D");
			labels.ShouldNotContain("B");
			labels.ShouldNotContain("C");
		}

		[TestMethod]
		public void TotalValue_IsCorrect()
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

			var instance = cut.Instance;

			var totalProp = typeof(DonutChart).GetProperty("TotalValue",
				BindingFlags.NonPublic | BindingFlags.Instance)
				?? throw new InvalidOperationException("TotalValue not found");

			var total = (int)totalProp.GetValue(instance)!;

			total.ShouldBe(60);
		}

		[TestMethod]
		public void InnerRadius_ComputedCorrectly_WhenDonut()
		{
			var cut = _bunit.Render<DonutChart>(p => p
				.Add(x => x.IsDonut, true)
				.Add(x => x.Thickness, 30)
				.Add(x => x.Data, new Dictionary<string, int> { ["A"] = 10 })
			);

			var instance = cut.Instance;

			var innerProp = typeof(DonutChart).GetProperty("InnerRadius",
				BindingFlags.NonPublic | BindingFlags.Instance)
				?? throw new InvalidOperationException("InnerRadius not found");

			var inner = (double)innerProp.GetValue(instance)!;

			inner.ShouldBe(60.0, 1e-6); // OuterRadius = 90 → 90 - 30 = 60
		}

		[TestMethod]
		public void InnerRadius_Zero_WhenPie()
		{
			var cut = _bunit.Render<DonutChart>(p => p
				.Add(x => x.IsDonut, false)
				.Add(x => x.Data, new Dictionary<string, int> { ["A"] = 10 })
			);

			var instance = cut.Instance;

			var innerProp = typeof(DonutChart).GetProperty("InnerRadius",
				BindingFlags.NonPublic | BindingFlags.Instance)
				?? throw new InvalidOperationException("InnerRadius not found");

			var inner = (double)innerProp.GetValue(instance)!;

			inner.ShouldBe(0.0, 1e-6);
		}
	}
}