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
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
			if( !eventData.dragging && !DraggedReferenceItem.InstanceItem ) // Don't show tooltip when there is an active DraggedReferenceItem
#else
			if( !eventData.dragging )
#endif
				drawer.Inspector.OnDrawerHovered( drawer, eventData, true );
		}

		public void OnPointerExit( PointerEventData eventData )
		{
			drawer.Inspector.OnDrawerHovered( drawer, eventData, false );
		}
	}
}