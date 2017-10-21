using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace RuntimeInspectorNamespace
{
	public class ObjectReferencePickerItem : RecycledListItem
	{
		public Object Reference { get; private set; }

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

					( (RectTransform) transform ).sizeDelta = new Vector2( 0f, Skin.LineHeight );

					int previewDimensions = Mathf.Max( 5, Skin.LineHeight - 7 );
                    texturePreviewLayoutElement.SetWidth( previewDimensions );
					texturePreviewLayoutElement.SetHeight( previewDimensions );

                    referenceNameText.SetSkinText( m_skin );

					IsSelected = m_isSelected;
				}
			}
		}

		[SerializeField]
		private Image background;

		[SerializeField]
		private RawImage texturePreview;
		private LayoutElement texturePreviewLayoutElement;

		[SerializeField]
		private Text referenceNameText;
		
		private bool m_isSelected = false;
		public bool IsSelected
		{
			get { return m_isSelected; }
			set
			{
				m_isSelected = value;

				if( m_isSelected )
				{
					background.color = Skin.SelectedItemBackgroundColor;
					referenceNameText.color = Skin.SelectedItemTextColor;
				}
				else
				{
					background.color = Color.clear;
					referenceNameText.color = Skin.TextColor;
				}
			}
		}
		
		void Awake()
		{
			texturePreviewLayoutElement = texturePreview.GetComponent<LayoutElement>();
            GetComponent<PointerEventListener>().PointerClick += (eventData) => OnClick();
		}

		public void SetContent( Object reference )
		{
			Reference = reference;
			referenceNameText.text = reference.GetNameWithType();

			Texture previewTex = reference.GetTexture();
			if( previewTex != null )
			{
				texturePreview.gameObject.SetActive( true );
				texturePreview.texture = previewTex;
			}
			else
				texturePreview.gameObject.SetActive( false );
		}
	}
}