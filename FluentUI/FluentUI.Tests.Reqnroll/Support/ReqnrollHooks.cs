namespace FluentUI.Tests.Reqnroll.Support;

using Reqnroll;

[Binding]
public class ReqnrollHooks
{
	[BeforeTestRun]
	public static void BeforeTestRun()
	{
		// Global setup
	}

	[AfterTestRun]
	public static void AfterTestRun()
	{
		// Global teardown
	}

	[BeforeScenario]
	public void BeforeScenario()
	{
		// Per-scenario setup
	}

	[AfterScenario]
	public void AfterScenario()
	{
		// Per-scenario cleanup
	}
}