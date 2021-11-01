using UnityEngine;
using UnityEngine.EventSystems;

namespace RuntimeInspectorNamespace
{
	public class TooltipArea : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
	{
		private TooltipListener tooltipListener;
		private ITooltipContent tooltipContent;

		public void Initialize( TooltipListener tooltipListener, ITooltipContent tooltipContent )
		{
			this.tooltipListener = tooltipListener;
			this.tooltipContent = tooltipContent;
		}

		public void OnPointerEnter( PointerEventData eventData )
		{
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
			if( !eventData.dragging && !DraggedReferenceItem.InstanceItem ) // Don't show tooltip when there is an active DraggedReferenceItem
#else
			if( !eventData.dragging )
#endif
				tooltipListener.OnDrawerHovered( tooltipContent, eventData, true );
		}

		public void OnPointerExit( PointerEventData eventData )
		{
			tooltipListener.OnDrawerHovered( tooltipContent, eventData, false );
		}
	}
}