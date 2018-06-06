using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RuntimeInspectorNamespace
{
	public abstract class HierarchyItem : MonoBehaviour
	{
		protected Color NORMAL_COLOR = Color.clear;
		protected const float INACTIVE_ITEM_TEXT_ALPHA = 0.57f;

		private bool initialized = false;

		private RuntimeHierarchy m_hierarchy;
		public RuntimeHierarchy Hierarchy
		{
			get { return m_hierarchy; }
			set
			{
				if( m_hierarchy != value )
				{
					m_hierarchy = value;

					if( !initialized )
					{
						Initialize();
						initialized = true;
					}
				}
			}
		}

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

					OnSkinChanged();
				}
			}
		}

		[SerializeField]
		protected LayoutGroup layoutGroup;

		[SerializeField]
		protected RectTransform contentTransform;

		[SerializeField]
		protected Text nameText;

		[SerializeField]
		protected RectTransform drawArea;

		[SerializeField]
		protected PointerEventListener clickListener;
		private Image background;

		[SerializeField]
		private PointerEventListener expandToggle;

		[SerializeField]
		private Image expandArrow; // Expand Arrow's sprite should look right at 0 rotation

		protected List<HierarchyItemTransform> children = new List<HierarchyItemTransform>( 4 );
		
		protected abstract bool IsValid { get; }
		protected abstract int ChildCount { get; }

		protected virtual bool IsActive
		{
			get { return true; }
			set { }
		}

		private bool m_isExpanded = false;
		public bool IsExpanded
		{
			get { return m_isExpanded; }
			set
			{
				m_isExpanded = value;
				drawArea.gameObject.SetActive( m_isExpanded );

				if( expandArrow != null )
					expandArrow.rectTransform.localEulerAngles = m_isExpanded ? new Vector3( 0f, 0f, -90f ) : Vector3.zero;

				if( m_isExpanded )
					Refresh();
				else
					ClearChildren();
			}
		}

		protected bool m_isSelected = false;
		public bool IsSelected
		{
			get { return m_isSelected; }
			set
			{
				m_isSelected = value;
				
				if( m_isSelected )
				{
					background.color = Skin.SelectedItemBackgroundColor;

					Color textColor = Skin.SelectedItemTextColor;
					textColor.a = IsActive ? 1f : INACTIVE_ITEM_TEXT_ALPHA;
                    nameText.color = textColor;
				}
				else
				{
					background.color = NORMAL_COLOR;

					Color textColor = Skin.TextColor;
					textColor.a = IsActive ? 1f : INACTIVE_ITEM_TEXT_ALPHA;
					nameText.color = textColor;
				}
			}
		}

		protected virtual void Initialize()
		{
			background = clickListener.GetComponent<Image>();

			clickListener.PointerClick += ( eventData ) => Hierarchy.OnClicked( this );
			expandToggle.PointerClick += ( eventData ) => IsExpanded = !m_isExpanded;

			IsExpanded = m_isExpanded;
		}

		public virtual void Unbind()
		{
			IsSelected = false;
			IsExpanded = false;

			ClearChildren();
			Hierarchy.PoolDrawer( this );
		}

		private void ClearChildren()
		{
			for( int i = 0; i < children.Count; i++ )
				children[i].Unbind();

			children.Clear();
		}

		protected virtual void OnSkinChanged()
		{
			layoutGroup.padding.left = Skin.IndentAmount;
			layoutGroup.padding.top = Skin.LineHeight;

			contentTransform.sizeDelta = new Vector2( 0f, Skin.LineHeight );
			
			nameText.SetSkinText( Skin );

			if( expandArrow != null )
				expandArrow.color = Skin.ExpandArrowColor;

			IsSelected = m_isSelected;

			for( int i = 0; i < children.Count; i++ )
				children[i].Skin = Skin;
        }

		public virtual void Refresh()
		{
			if( !IsValid )
			{
				Unbind();
				return;
			}

			RefreshContent();

			if( m_isExpanded )
			{
				for( int i = children.Count - 1; i >= 0; i-- )
				{
					if( children[i].BoundTransform.IsNull() )
					{
						children[i].Unbind();
						children.RemoveAt( i );
					}
				}

				int index;
				int childCount = ChildCount;
				for( index = 0; index < childCount; index++ )
				{
					Transform child = GetChild( index );
					if( children.Count <= index )
						GenerateChildItem( child, index );
					else if( children[index].BoundTransform != child )
					{
						int childIndex;
						for( childIndex = 0; childIndex < children.Count; childIndex++ )
						{
							if( children[childIndex].BoundTransform == child )
								break;
						}

						if( childIndex == children.Count )
							GenerateChildItem( child, index );
						else
						{
							HierarchyItemTransform childItem = children[childIndex];

							children.RemoveAt( childIndex );
							children.Insert( index, childItem );

							childItem.transform.SetSiblingIndex( index );
						}
					}

					( (HierarchyItem) children[index] ).IsActive = children[index].BoundTransform.gameObject.activeInHierarchy;
					children[index].Refresh();
				}

				for( int i = children.Count - 1; i >= index; i-- )
				{
					children[i].Unbind();
					children.RemoveAt( i );
				}
			}

			if( ChildCount == 0 )
			{
				if( expandToggle.gameObject.activeSelf )
				{
					expandToggle.gameObject.SetActive( false );
					IsExpanded = false;
				}
			}
			else if( !expandToggle.gameObject.activeSelf )
				expandToggle.gameObject.SetActive( true );
		}

		public void RefreshNameOf( Transform target )
		{
			for( int i = children.Count - 1; i >= 0; i-- )
			{
				if( children[i].BoundTransform == target )
					children[i].nameText.text = target.name;

				children[i].RefreshNameOf( target );
			}
		}

		protected virtual void RefreshContent() { }

		private void GenerateChildItem( Transform child, int index )
		{
			HierarchyItemTransform item = Hierarchy.InstantiateTransformDrawer( drawArea );
			item.transform.SetSiblingIndex( index );
			item.BindTo( child );

			children.Insert( index, item );
		}

		//public HierarchyItem SelectTransform( Transform target, Transform nextInPath = null )
		//{
		//	bool wasExpanded = IsExpanded;
		//	if( !wasExpanded )
		//		IsExpanded = true;
		//	else
		//		Refresh();

		//	if( nextInPath == null )
		//		nextInPath = target.root;

		//	HierarchyItem result = null;
		//	for( int i = 0; i < children.Count; i++ )
		//	{
		//		if( children[i].BoundTransform == target )
		//		{
		//			Hierarchy.OnClicked( children[i] );
		//			result = children[i];

		//			break;
		//		}
		//		else if( children[i].BoundTransform == nextInPath )
		//		{
		//			Transform next = target;
		//			Transform parent = next.parent;
		//			while( parent != null && parent != nextInPath )
		//			{
		//				next = parent;
		//				parent = next.parent;
		//			}

		//			if( parent != null )
		//				result = children[i].SelectTransform( target, next );

		//			break;
		//		}
		//	}

		//	if( result == null && !wasExpanded )
		//		IsExpanded = false;

		//	return result;
		//}

		public HierarchyItem SelectTransform( Transform target, Transform nextInPath = null )
		{
			bool isInitSearch = nextInPath == null;
            if( isInitSearch )
				nextInPath = target.root;

			RefreshContent();

			int childIndex = IndexOf( nextInPath );
			if( childIndex < 0 )
			{
				if( isInitSearch && this is HierarchyItemRoot && ( (HierarchyItemRoot) this ).Content is HierarchyRootPseudoScene )
				{
					nextInPath = target;
					childIndex = IndexOf( nextInPath );
					while( childIndex < 0 && nextInPath != null )
					{
						nextInPath = nextInPath.parent;
						childIndex = IndexOf( nextInPath );
					}

					if( childIndex < 0 )
						return null;
				}
				else
					return null;
			}
			
			bool wasExpanded = IsExpanded;
			if( !wasExpanded )
				IsExpanded = true;

			HierarchyItemTransform childItem = children[childIndex];
            if( childItem.BoundTransform == target )
			{
				Hierarchy.OnClicked( childItem );
				return childItem;
			}

			HierarchyItem result = null;
			if( childItem.BoundTransform == nextInPath )
			{
				Transform next = target;
				Transform parent = next.parent;
				while( parent != null && parent != nextInPath )
				{
					next = parent;
					parent = next.parent;
				}

				if( parent != null )
				{
					result = childItem.SelectTransform( target, next );

					if( result.IsNull() )
						result = null;
				}
			}

			if( result.IsNull() && !wasExpanded )
				IsExpanded = false;

			return result;
		}

		protected int IndexOf( Transform transform )
		{
			int childCount = ChildCount;
			for( int i = 0; i < childCount; i++ )
			{
				if( GetChild( i ) == transform )
					return i;
			}

			return -1;
		}

		protected abstract Transform GetChild( int index );
	}
}