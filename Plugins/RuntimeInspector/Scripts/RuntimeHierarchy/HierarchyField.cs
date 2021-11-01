using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RuntimeInspectorNamespace
{
	public class HierarchyField : RecycledListItem, ITooltipContent
	{
		private enum ExpandedState { Collapsed = 0, Expanded = 1, ArrowHidden = 2 };

		private const float INACTIVE_ITEM_TEXT_ALPHA = 0.57f;
		private const float TEXT_X_OFFSET = 35f;

#pragma warning disable 0649
		[SerializeField]
		private RectTransform contentTransform;

		[SerializeField]
		private Text nameText;

		[SerializeField]
		private PointerEventListener clickListener;

		[SerializeField]
		private PointerEventListener expandToggle;

		[SerializeField]
		private Image expandArrow;

		[SerializeField]
		private Toggle multiSelectionToggle;

		[SerializeField]
		private Image multiSelectionToggleBackground;
#pragma warning restore 0649

		private RectTransform rectTransform;
		private Image background;

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

					rectTransform.sizeDelta = new Vector2( 0f, Skin.LineHeight );

					nameText.SetSkinText( Skin );
					expandArrow.color = Skin.ExpandArrowColor;

					nameText.rectTransform.anchoredPosition = new Vector2( Skin.ExpandArrowSpacing + Skin.LineHeight * 0.75f, 0f );
					( (RectTransform) expandToggle.transform ).sizeDelta = new Vector2( Skin.LineHeight, Skin.LineHeight );
					( (RectTransform) multiSelectionToggle.transform ).sizeDelta = new Vector2( Skin.LineHeight * 0.8f, Skin.LineHeight * 0.8f );

					multiSelectionToggle.graphic.color = Skin.ToggleCheckmarkColor;
					multiSelectionToggleBackground.color = Skin.InputFieldNormalBackgroundColor;
				}
			}
		}

		private bool m_isSelected;
		public bool IsSelected
		{
			get { return m_isSelected; }
			set
			{
				m_isSelected = value;

				Color textColor;
				if( m_isSelected )
				{
					background.color = Skin.SelectedItemBackgroundColor;
					textColor = Skin.SelectedItemTextColor;
				}
				else
				{
					background.color = Data.Depth == 0 ? Skin.BackgroundColor.Tint( 0.075f ) : Color.clear;
					textColor = Skin.TextColor;
				}

				textColor.a = m_isActive ? 1f : INACTIVE_ITEM_TEXT_ALPHA;
				nameText.color = textColor;
				multiSelectionToggle.isOn = m_isSelected;
			}
		}

		private bool m_isActive;
		private bool IsActive
		{
			get { return m_isActive; }
			set
			{
				if( m_isActive != value )
				{
					m_isActive = value;

					Color color = nameText.color;
					color.a = m_isActive ? 1f : INACTIVE_ITEM_TEXT_ALPHA;
					nameText.color = color;
				}
			}
		}

		public bool MultiSelectionToggleVisible
		{
			get { return multiSelectionToggle.gameObject.activeSelf; }
			set
			{
				if( Data == null || Data.Depth <= 0 )
					value = false;

				if( multiSelectionToggle.gameObject.activeSelf != value )
				{
					multiSelectionToggle.gameObject.SetActive( value );
					contentTransform.anchoredPosition = new Vector2( Skin.IndentAmount * Data.Depth + ( value ? Skin.LineHeight * 0.8f : 0f ), 0f );
				}
			}
		}

		private ExpandedState m_isExpanded = ExpandedState.Collapsed;
		private ExpandedState IsExpanded
		{
			get { return m_isExpanded; }
			set
			{
				if( m_isExpanded != value )
				{
					m_isExpanded = value;

					if( m_isExpanded == ExpandedState.ArrowHidden )
						expandToggle.gameObject.SetActive( false );
					else
					{
						expandToggle.gameObject.SetActive( true );
						expandArrow.rectTransform.localEulerAngles = m_isExpanded == ExpandedState.Expanded ? new Vector3( 0f, 0f, -90f ) : Vector3.zero;
					}
				}
			}
		}

		bool ITooltipContent.IsActive { get { return this && gameObject.activeSelf; } }
		string ITooltipContent.TooltipText { get { return Data.Name; } }

		public float PreferredWidth { get; private set; }

		public RuntimeHierarchy Hierarchy { get; private set; }
		public HierarchyData Data { get; private set; }

		public void Initialize( RuntimeHierarchy hierarchy )
		{
			Hierarchy = hierarchy;

			rectTransform = (RectTransform) transform;
			background = clickListener.GetComponent<Image>();

			if( hierarchy.ShowTooltips )
				clickListener.gameObject.AddComponent<TooltipArea>().Initialize( hierarchy.TooltipListener, this );

			expandToggle.PointerClick += ( eventData ) => ToggleExpandedState();
			clickListener.PointerClick += ( eventData ) => OnClick();
			clickListener.PointerDown += OnPointerDown;
			clickListener.PointerUp += OnPointerUp;
		}

		public void SetContent( HierarchyData data )
		{
			Data = data;

			contentTransform.anchoredPosition = new Vector2( Skin.IndentAmount * data.Depth + ( MultiSelectionToggleVisible ? Skin.LineHeight * 0.8f : 0f ), 0f );
			background.sprite = data.Depth == 0 ? Hierarchy.SceneDrawerBackground : Hierarchy.TransformDrawerBackground;

			RefreshName();
		}

		private void ToggleExpandedState()
		{
			Data.IsExpanded = !Data.IsExpanded;
		}

		public void Refresh()
		{
			IsActive = Data.IsActive;
			IsExpanded = Data.CanExpand ? ( Data.IsExpanded ? ExpandedState.Expanded : ExpandedState.Collapsed ) : ExpandedState.ArrowHidden;
		}

		public void RefreshName()
		{
			nameText.text = Data.Name;

			if( Hierarchy.ShowHorizontalScrollbar )
			{
				LayoutRebuilder.ForceRebuildLayoutImmediate( nameText.rectTransform );
				PreferredWidth = Data.Depth * m_skin.IndentAmount + TEXT_X_OFFSET + nameText.rectTransform.sizeDelta.x;
			}
		}

		private void OnPointerDown( PointerEventData eventData )
		{
			Hierarchy.OnDrawerPointerEvent( this, eventData, true );
		}

		private void OnPointerUp( PointerEventData eventData )
		{
			Hierarchy.OnDrawerPointerEvent( this, eventData, false );
		}
	}
}