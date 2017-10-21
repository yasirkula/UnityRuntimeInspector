using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RuntimeInspectorNamespace
{
	public class DraggedReferenceSourceUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
	{
		private Object m_reference;
		public Object Reference
		{
			get { return m_reference; }
			set { m_reference = value; }
		}

		[SerializeField]
		private UISkin draggedReferenceSkin;

		[SerializeField]
		private float holdTime = 0.4f;

		private IEnumerator pointerHeldCoroutine = null;

		public void OnPointerDown( PointerEventData eventData )
		{
			if( pointerHeldCoroutine != null )
				return;

			if( m_reference.IsNull() )
				return;

			pointerHeldCoroutine = CreateReferenceItemCoroutine( eventData );
			StartCoroutine( pointerHeldCoroutine );
		}

		public void OnPointerUp( PointerEventData eventData )
		{
			if( pointerHeldCoroutine != null )
			{
				StopCoroutine( pointerHeldCoroutine );
				pointerHeldCoroutine = null;
			}
		}

		private IEnumerator CreateReferenceItemCoroutine( PointerEventData eventData )
		{
			yield return new WaitForSecondsRealtime( holdTime );

			if( !m_reference.IsNull() )
				RuntimeInspectorUtils.CreateDraggedReferenceItem( m_reference, eventData, draggedReferenceSkin );
		}
	}
}