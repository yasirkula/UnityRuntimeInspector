using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

namespace RuntimeInspectorNamespace
{
	public class DraggedReferenceItem : PopupBase, IDragHandler, IEndDragHandler
	{
		private object[] m_references;
		public object[] References { get { return m_references; } }

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
		// In new Input System, it is just not possible to change a PointerEventData's pointerDrag and dragging variables inside Update/LateUpdate,
		// EventSystemUIInputModule will reverse these changes immediately. So we'll allow only a single DraggedReferenceItem
		// with the new Input System and track its PointerEventData manually using Pointer.current
		internal static DraggedReferenceItem InstanceItem { get; private set; }

		private readonly System.Collections.Generic.List<RaycastResult> hoveredUIElements = new System.Collections.Generic.List<RaycastResult>( 4 );
#endif

		public void SetContent( object[] references, PointerEventData draggingPointer )
		{
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
			if( InstanceItem )
				InstanceItem.DestroySelf();

			InstanceItem = this;

			draggingPointer = new PointerEventData( EventSystem.current )
			{
				pressPosition = draggingPointer.pressPosition,
				position = draggingPointer.position,
				delta = draggingPointer.delta
			};
#endif

			m_references = references;
			label.text = ( references.Length == 1 ) ? references[0].GetNameWithType() : string.Concat( references[0].GetNameWithType(), " (and ", ( references.Length - 1 ).ToString( RuntimeInspectorUtils.numberFormat ), " more)" );

			draggingPointer.pointerDrag = gameObject;
			draggingPointer.dragging = true;

			SetPointer( draggingPointer );
		}

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
		private void LateUpdate()
		{
			if( Pointer.current == null || !Pointer.current.press.isPressed )
			{
				// We must execute OnDrop manually
				if( EventSystem.current )
				{
					hoveredUIElements.Clear();
					EventSystem.current.RaycastAll( pointer, hoveredUIElements );

					int i = 0;
					while( i < hoveredUIElements.Count && !ExecuteEvents.ExecuteHierarchy( hoveredUIElements[i].gameObject, pointer, ExecuteEvents.dropHandler ) )
						i++;
				}

				OnEndDrag( pointer );
			}
			else
			{
				Vector2 pointerPos = Pointer.current.position.ReadValue();
				Vector2 pointerDelta = pointerPos - pointer.position;
				if( pointerDelta.x != 0f || pointerDelta.y != 0f )
				{
					pointer.position = pointerPos;
					pointer.delta = pointerDelta;

					OnDrag( pointer );
				}
			}
		}
#endif

		protected override void DestroySelf()
		{
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
			if( InstanceItem == this )
				InstanceItem = null;
#endif

			RuntimeInspectorUtils.PoolDraggedReferenceItem( this );
		}

		public void OnDrag( PointerEventData eventData )
		{
			if( eventData.pointerId != pointer.pointerId )
				return;

			RepositionSelf();
		}

		public void OnEndDrag( PointerEventData eventData )
		{
			DestroySelf();
		}
	}
}