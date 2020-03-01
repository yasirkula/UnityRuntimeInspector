using UnityEngine;
using UnityEngine.EventSystems;

namespace RuntimeInspectorNamespace
{
	public class DraggedReferenceItem : PopupBase, IDragHandler, IEndDragHandler
	{
		private Object m_reference;
		public Object Reference { get { return m_reference; } }

		public void SetContent( Object reference, PointerEventData draggingPointer )
		{
			m_reference = reference;
			label.text = reference.GetNameWithType();

			draggingPointer.pointerDrag = gameObject;
			draggingPointer.dragging = true;

			SetPointer( draggingPointer );
		}

		protected override void DestroySelf()
		{
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
			RuntimeInspectorUtils.PoolDraggedReferenceItem( this );
		}
	}
}