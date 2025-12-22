using Bunit;

namespace DonutChartSolution.Tests
{
	public static class DonutChartTestExtensions
	{
		/// <summary>
		/// Globally configures JSInterop for DonutChart so tests do not fail
		/// when the component calls JS during OnAfterRenderAsync.
		/// </summary>
		public static void SetupDonutChartInterop(this BunitJSInterop js)
		{
			js.SetupVoid("donutChart.registerSliceClicks", _ => true);
		}
	}
}
