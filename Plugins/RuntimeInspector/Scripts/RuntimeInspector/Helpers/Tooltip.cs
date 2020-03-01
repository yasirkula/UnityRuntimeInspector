using UnityEngine.EventSystems;

namespace RuntimeInspectorNamespace
{
	public class Tooltip : PopupBase
	{
		public void SetContent( string tooltip, PointerEventData pointer )
		{
			label.text = tooltip;
			SetPointer( pointer );
		}

		protected override void DestroySelf()
		{
			gameObject.SetActive( false );
		}
	}
}