using Microsoft.JSInterop;

namespace BlazorApp1.Components.Shared.Select
{
	public class OutsideClickService
	{
		private DotNetObjectReference<MultiSelectCombo>? _current;

		public void Register(DotNetObjectReference<MultiSelectCombo> reference)
		{
			_current = reference;
		}

		[JSInvokable]
		public async Task NotifyOutsideClick()
		{
			if (_current != null)
				await Task.Run(() => _current.Value.CloseDropdown());
		}
	}

}
