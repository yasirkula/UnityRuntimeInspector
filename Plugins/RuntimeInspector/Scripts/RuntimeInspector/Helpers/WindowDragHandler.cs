using UnityEngine;
using UnityEngine.EventSystems;

namespace RuntimeInspectorNamespace
{
	public class WindowDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
	{
		private const int NON_EXISTING_TOUCH = -98456;
		
		private RectTransform rectTransform;

		private int pointerId = NON_EXISTING_TOUCH;
		private Vector2 initialTouchPos;

		void Awake()
		{
			rectTransform = (RectTransform) transform;
		}

		public void OnBeginDrag( PointerEventData eventData )
		{
			if( pointerId != NON_EXISTING_TOUCH )
			{
				eventData.pointerDrag = null;
				return;
			}

			pointerId = eventData.pointerId;
			RectTransformUtility.ScreenPointToLocalPointInRectangle( rectTransform, eventData.position, eventData.pressEventCamera, out initialTouchPos );
		}

		public void OnDrag( PointerEventData eventData )
		{
			if( eventData.pointerId != pointerId )
				return;

			Vector2 touchPos;
			RectTransformUtility.ScreenPointToLocalPointInRectangle( rectTransform, eventData.position, eventData.pressEventCamera, out touchPos );

			rectTransform.anchoredPosition += touchPos - initialTouchPos;
		}

		public void OnEndDrag( PointerEventData eventData )
		{
			if( eventData.pointerId != pointerId )
				return;

			pointerId = NON_EXISTING_TOUCH;
		}
	}
}