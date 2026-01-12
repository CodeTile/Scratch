using FluentUI.Components.Shared;

namespace FluentUI.Components.Tests
{// Subclass to expose protected OnParametersSet for testing
	public class TestableCheckBoxList<TItem> : CheckBoxList<TItem>
	{
		public void OnParametersSetPublic() => base.OnParametersSet();
	}
}