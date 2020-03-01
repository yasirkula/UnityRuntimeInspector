using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RuntimeInspectorNamespace
{
	public abstract class PopupBase : MonoBehaviour
	{
		private const float POINTER_VALIDATE_INTERVAL = 5f;

#pragma warning disable 0649
		[SerializeField]
		private LayoutElement borderLayoutElement;

		[SerializeField]
		private Image background;

		[SerializeField]
		protected Text label;
#pragma warning restore 0649

		private RectTransform rectTransform;
		private RectTransform canvasTransform;
		private Camera worldCamera;

		protected PointerEventData pointer;
		private float nextPointerValidation;

		private int m_skinVersion = 0;
		private UISkin m_skin;
		public UISkin Skin
		{
			get { return m_skin; }
			set
			{
				if( m_skin != value || m_skinVersion != m_skin.Version )
				{
					m_skin = value;
					m_skinVersion = m_skin.Version;

					borderLayoutElement.SetHeight( m_skin.LineHeight * 2.5f );
					background.GetComponent<LayoutElement>().minHeight = m_skin.LineHeight;

					float alpha = background.color.a;
					Color skinColor = m_skin.InputFieldNormalBackgroundColor.Tint( 0.05f );
					skinColor.a = alpha;
					background.color = skinColor;

					label.SetSkinInputFieldText( m_skin );
				}
			}
		}

		public void Initialize( Canvas canvas )
		{
			rectTransform = (RectTransform) transform;
			canvasTransform = (RectTransform) canvas.transform;

			if( canvas.renderMode == RenderMode.ScreenSpaceOverlay || ( canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera == null ) )
				worldCamera = null;
			else
				worldCamera = canvas.worldCamera ? canvas.worldCamera : Camera.main;
		}

		protected void SetPointer( PointerEventData pointer )
		{
			this.pointer = pointer;
			nextPointerValidation = POINTER_VALIDATE_INTERVAL;

			RepositionSelf();
		}

		protected void RepositionSelf()
		{
			Vector2 touchPos;
			if( RectTransformUtility.ScreenPointToLocalPointInRectangle( canvasTransform, pointer.position, worldCamera, out touchPos ) )
				rectTransform.anchoredPosition = touchPos;
		}

		protected abstract void DestroySelf();

		private void Update()
		{
			nextPointerValidation -= Time.unscaledDeltaTime;
			if( nextPointerValidation <= 0f )
			{
				nextPointerValidation = POINTER_VALIDATE_INTERVAL;

				if( !pointer.IsPointerValid() )
					DestroySelf();
			}
		}
	}
}