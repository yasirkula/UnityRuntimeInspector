using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using Pointer = UnityEngine.InputSystem.Pointer;
#endif

namespace RuntimeInspectorNamespace
{
	public interface ITooltipContent
	{
		bool IsActive { get; }
		string TooltipText { get; }
	}

	public interface ITooltipManager
	{
		UISkin Skin { get; }
		Canvas Canvas { get; }
		float TooltipDelay { get; }
	}

	public class TooltipListener : MonoBehaviour
	{
		private ITooltipManager manager;
		private ITooltipContent hoveredDrawer;
		private PointerEventData hoveringPointer;
		private float hoveredDrawerTooltipShowTime;

		public void Initialize( ITooltipManager manager )
		{
			this.manager = manager;
		}

		private void Update()
		{
			// Check if a pointer has remained static over a drawer for a while; if so, show a tooltip
			float time = Time.realtimeSinceStartup;
			if( hoveringPointer != null )
			{
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
				// PointerEventData.delta isn't set to (0,0) for static pointers in the new Input System, so we use the active Pointer's delta instead
				// The default value isn't Vector2.zero but Vector2.one because we don't want to show tooltip if there is no pointer
				Vector2 pointerDelta = Pointer.current != null ? Pointer.current.delta.ReadValue() : Vector2.one;
#else
				Vector2 pointerDelta = hoveringPointer.delta;
#endif
				if( pointerDelta.x != 0f || pointerDelta.y != 0f )
					hoveredDrawerTooltipShowTime = time + manager.TooltipDelay;
				else if( time > hoveredDrawerTooltipShowTime )
				{
					// Make sure that everything is OK
					if( !hoveredDrawer.IsActive )
					{
						hoveredDrawer = null;
						hoveringPointer = null;
					}
					else
					{
						RuntimeInspectorUtils.ShowTooltip( hoveredDrawer.TooltipText, hoveringPointer, manager.Skin, manager.Canvas );

						// Don't show the tooltip again until the pointer moves
						hoveredDrawerTooltipShowTime = float.PositiveInfinity;
					}
				}
			}
		}

		internal void OnDrawerHovered( ITooltipContent drawer, PointerEventData pointer, bool isHovering )
		{
			// Hide tooltip if it is currently visible
			RuntimeInspectorUtils.HideTooltip();

			if( isHovering )
			{
				hoveredDrawer = drawer;
				hoveringPointer = pointer;
				hoveredDrawerTooltipShowTime = Time.realtimeSinceStartup + manager.TooltipDelay;
			}
			else if( drawer == null || hoveredDrawer == drawer )
			{
				hoveredDrawer = null;
				hoveringPointer = null;
			}
		}
	}
}