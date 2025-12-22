using System.Reflection;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using DonutChartSolution.Components.Shared;

namespace DonutChartSolution.Tests.Components.Shared
{
	[TestClass]
	public class DonutChartGeometryTests
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
		// Reflection helpers
		// ---------------------------------------------------------

		private static string InvokeBuildDonutSlicePath(DonutChart instance, double startAngle, double sweepAngle)
		{
			var method = typeof(DonutChart)
				.GetMethod("BuildDonutSlicePath", BindingFlags.NonPublic | BindingFlags.Instance)
				?? throw new InvalidOperationException("BuildDonutSlicePath not found");

			return (string)method.Invoke(instance, new object[] { startAngle, sweepAngle })!;
		}

		private static double InvokeDegreesToRadians(double degrees)
		{
			var method = typeof(DonutChart)
				.GetMethod("DegreesToRadians", BindingFlags.NonPublic | BindingFlags.Static)
				?? throw new InvalidOperationException("DegreesToRadians not found");

			return (double)method.Invoke(null, new object[] { degrees })!;
		}

		// ---------------------------------------------------------
		// Tests
		// ---------------------------------------------------------

		[TestMethod]
		public void DegreesToRadians_ConvertsCorrectly()
		{
			var rad = InvokeDegreesToRadians(180);
			rad.ShouldBe(Math.PI, 1e-10);
		}

		[TestMethod]
		public void BuildDonutSlicePath_PieSlice_HasMoveLineArcClose()
		{
			var cut = _bunit.Render<DonutChart>(p => p
				.Add(x => x.IsDonut, false)
				.Add(x => x.Data, new Dictionary<string, int> { ["A"] = 10 })
			);

			var instance = cut.Instance;
			var path = InvokeBuildDonutSlicePath(instance, -90, 90);

			path.ShouldStartWith("M 100 100 ");
			path.ShouldContain(" L ");
			path.ShouldContain(" A ");
			path.TrimEnd().ShouldEndWith("Z");
		}

		[TestMethod]
		public void BuildDonutSlicePath_DonutSlice_HasOuterAndInnerArcs()
		{
			var cut = _bunit.Render<DonutChart>(p => p
				.Add(x => x.IsDonut, true)
				.Add(x => x.Thickness, 40)
				.Add(x => x.Data, new Dictionary<string, int> { ["A"] = 10 })
			);

			var instance = cut.Instance;
			var path = InvokeBuildDonutSlicePath(instance, -90, 120);

			path.ShouldContain("A 90 90");  // outer arc
			path.ShouldContain("A 50 50");  // inner arc (90 - 40)
			path.ShouldContain(" L ");
		}

		[TestMethod]
		public void BuildDonutSlicePath_UsesLargeArcFlag_WhenSweepGreaterThan180()
		{
			var cut = _bunit.Render<DonutChart>(p => p
				.Add(x => x.IsDonut, true)
				.Add(x => x.Data, new Dictionary<string, int> { ["A"] = 10 })
			);

			var instance = cut.Instance;
			var path = InvokeBuildDonutSlicePath(instance, 0, 270);

			path.ShouldContain("A 90 90 0 1 1");
		}

		[TestMethod]
		public void BuildDonutSlicePath_UsesSmallArcFlag_WhenSweepLessThanOrEqual180()
		{
			var cut = _bunit.Render<DonutChart>(p => p
				.Add(x => x.IsDonut, true)
				.Add(x => x.Data, new Dictionary<string, int> { ["A"] = 10 })
			);

			var instance = cut.Instance;
			var path = InvokeBuildDonutSlicePath(instance, 0, 90);

			path.ShouldContain("A 90 90 0 0 1");
		}
	}
}
