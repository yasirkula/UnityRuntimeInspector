using UnityEngine;
using UnityEngine.EventSystems;

namespace RuntimeInspectorNamespace
{
	public class TooltipArea : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
	{
		private InspectorField drawer;

		public void Initialize( InspectorField drawer )
		{
			this.drawer = drawer;
		}

		public void OnPointerEnter( PointerEventData eventData )
		{
			if( !eventData.dragging )
				drawer.Inspector.OnDrawerHovered( drawer, eventData, true );
		}

		public void OnPointerExit( PointerEventData eventData )
		{
			drawer.Inspector.OnDrawerHovered( drawer, eventData, false );
		}
	}
}