using RuntimeInspectorNamespace;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RuntimeInspectorNamespace
{
	public class DraggedReferenceItem : MonoBehaviour, IDragHandler, IEndDragHandler
	{
		private RectTransform rectTransform;

		private Canvas canvas;
		private RectTransform canvasTransform;

		[SerializeField]
		private LayoutElement borderLayoutElement;

		[SerializeField]
		private Image background;

		[SerializeField]
		private Text referenceName;

		private Object m_reference;
		public Object Reference { get { return m_reference; } }

		private int pointerId = -98764;

		public void Initialize( Canvas canvas, Object reference, PointerEventData draggingPointer, UISkin skin )
		{
			rectTransform = (RectTransform) transform;

			this.canvas = canvas;
			canvasTransform = (RectTransform) canvas.transform;

			m_reference = reference;
			referenceName.text = reference.GetNameWithType();

			pointerId = draggingPointer.pointerId;

			if( skin != null )
			{
				borderLayoutElement.SetHeight( skin.LineHeight * 2.5f );
				background.GetComponent<LayoutElement>().minHeight = skin.LineHeight;

				float alpha = background.color.a;
				Color skinColor = skin.InputFieldNormalBackgroundColor.Tint( 0.05f );
				skinColor.a = alpha;
				background.color = skinColor;

				referenceName.SetSkinInputFieldText( skin );
			}

			OnDrag( draggingPointer );

            draggingPointer.pointerDrag = gameObject;
			draggingPointer.dragging = true;
		}

		public void OnDrag( PointerEventData eventData )
		{
			if( eventData.pointerId != pointerId )
				return;

			Vector2 touchPos;
			RectTransformUtility.ScreenPointToLocalPointInRectangle( canvasTransform, eventData.position, canvas.worldCamera, out touchPos );

			rectTransform.anchoredPosition = touchPos;
		}

		public void OnEndDrag( PointerEventData eventData )
		{
			Destroy( gameObject );
		}
	}
}