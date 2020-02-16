using UnityEngine;
using UnityEngine.EventSystems;

namespace RuntimeInspectorNamespace
{
	public class PointerEventListener : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
	{
		public delegate void PointerEvent( PointerEventData eventData );

		public event PointerEvent PointerDown, PointerUp, PointerClick;

		void IPointerDownHandler.OnPointerDown( PointerEventData eventData )
		{
			if( PointerDown != null )
				PointerDown( eventData );
		}

		void IPointerUpHandler.OnPointerUp( PointerEventData eventData )
		{
			if( PointerUp != null )
				PointerUp( eventData );
		}

		void IPointerClickHandler.OnPointerClick( PointerEventData eventData )
		{
			if( PointerClick != null )
				PointerClick( eventData );
		}
	}
}