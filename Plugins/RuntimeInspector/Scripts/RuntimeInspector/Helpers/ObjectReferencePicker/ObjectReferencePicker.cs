using System;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.UI;
using System.Collections.Generic;

namespace RuntimeInspectorNamespace
{
	public class ObjectReferencePicker : SkinnedWindow, IListViewAdapter
	{
		private const string SPRITE_ATLAS_PREFIX = "SpriteAtlasTexture-";

		private static ObjectReferencePicker m_instance;
		public static ObjectReferencePicker Instance
		{
			get
			{
				if( !m_instance )
				{
					m_instance = Instantiate( Resources.Load<ObjectReferencePicker>( "RuntimeInspector/ObjectReferencePicker" ) );
					m_instance.gameObject.SetActive( false );

					RuntimeInspectorUtils.IgnoredTransformsInHierarchy.Add( m_instance.transform );
				}

				return m_instance;
			}
		}

		public delegate void OnReferenceChanged( Object reference );
		private OnReferenceChanged onReferenceChanged;

#pragma warning disable 0649
		[SerializeField]
		private Image panel;

		[SerializeField]
		private Image scrollbar;

		[SerializeField]
		private InputField searchBar;

		[SerializeField]
		private Image searchIcon;

		[SerializeField]
		private Image searchBarBackground;

		[SerializeField]
		private Text selectPromptText;

		[SerializeField]
		private LayoutElement searchBarLayoutElement;

		[SerializeField]
		private LayoutElement buttonsLayoutElement;

		[SerializeField]
		private Button cancelButton;

		[SerializeField]
		private Button okButton;

		[SerializeField]
		private RecycledListView listView;

		[SerializeField]
		private Image listViewBackground;

		[SerializeField]
		private ObjectReferencePickerItem referenceItemPrefab;
#pragma warning restore 0649

		private Canvas referenceCanvas;

		private readonly List<Object> references = new List<Object>( 64 );
		private readonly List<Object> filteredReferences = new List<Object>( 64 );

		private Object initialValue;

		private Object currentlySelectedObject;
		private ObjectReferencePickerItem currentlySelectedItem;

		int IListViewAdapter.Count { get { return filteredReferences.Count; } }
		float IListViewAdapter.ItemHeight { get { return Skin.LineHeight; } }

		protected override void Awake()
		{
			base.Awake();

			listView.SetAdapter( this );
			searchBar.onValueChanged.AddListener( OnSearchTextChanged );

			cancelButton.onClick.AddListener( Cancel );
			okButton.onClick.AddListener( Close );
		}

		public void Show( OnReferenceChanged onReferenceChanged, Type referenceType, Object[] references, Object initialReference, Canvas referenceCanvas )
		{
			initialValue = initialReference;
			this.onReferenceChanged = onReferenceChanged;

			if( referenceCanvas && this.referenceCanvas != referenceCanvas )
			{
				this.referenceCanvas = referenceCanvas;

				Canvas canvas = GetComponent<Canvas>();
				canvas.CopyValuesFrom( referenceCanvas );
				canvas.sortingOrder = Mathf.Max( 1000, referenceCanvas.sortingOrder + 100 );
			}

			panel.rectTransform.anchoredPosition = Vector2.zero;
			gameObject.SetActive( true );

			selectPromptText.text = "Select " + referenceType.Name;
			currentlySelectedObject = initialReference;

			GenerateReferenceItems( references, referenceType );
		}

		public void Cancel()
		{
			if( currentlySelectedObject != initialValue && onReferenceChanged != null )
				onReferenceChanged( initialValue );

			Close();
		}

		public void Close()
		{
			onReferenceChanged = null;
			initialValue = null;
			currentlySelectedObject = null;
			currentlySelectedItem = null;

			references.Clear();
			filteredReferences.Clear();

			gameObject.SetActive( false );
		}

		protected override void RefreshSkin()
		{
			panel.color = Skin.WindowColor;
			listViewBackground.color = Skin.BackgroundColor;

			scrollbar.color = Skin.ScrollbarColor;

			selectPromptText.SetSkinText( Skin );
			searchBar.textComponent.SetSkinButtonText( Skin );

			searchBarBackground.color = Skin.ButtonBackgroundColor;
			searchIcon.color = Skin.ButtonTextColor;

			searchBarLayoutElement.SetHeight( Skin.LineHeight );
			buttonsLayoutElement.SetHeight( Mathf.Min( 45f, Skin.LineHeight * 1.5f ) );

			cancelButton.SetSkinButton( Skin );
			okButton.SetSkinButton( Skin );

			listView.ResetList();
		}

		private void GenerateReferenceItems( Object[] references, Type referenceType )
		{
			this.references.Clear();
			filteredReferences.Clear();
			searchBar.text = string.Empty;

			this.references.Add( null );
			Array.Sort( references, ( ref1, ref2 ) => ref1.GetName().CompareTo( ref2.GetName() ) );

			bool isTexture = referenceType == typeof( Texture ) || referenceType == typeof( Texture ) || referenceType == typeof( Sprite );
			for( int i = 0; i < references.Length; i++ )
			{
				if( !references[i] )
					continue;

				if( references[i].hideFlags != HideFlags.None && references[i].hideFlags != HideFlags.NotEditable &&
					references[i].hideFlags != HideFlags.HideInHierarchy && references[i].hideFlags != HideFlags.HideInInspector )
					continue;

				if( isTexture && references[i].name.StartsWith( SPRITE_ATLAS_PREFIX ) )
					continue;

				this.references.Add( references[i] );
			}

			OnSearchTextChanged( string.Empty );

			listView.UpdateList();
		}

		RecycledListItem IListViewAdapter.CreateItem( Transform parent )
		{
			ObjectReferencePickerItem item = (ObjectReferencePickerItem) Instantiate( referenceItemPrefab, parent, false );
			item.Skin = Skin;

			return item;
		}

		private void OnSearchTextChanged( string value )
		{
			filteredReferences.Clear();

			value = value.ToLowerInvariant();
			for( int i = 0; i < references.Count; i++ )
			{
				if( references[i].GetName().ToLowerInvariant().Contains( value ) )
					filteredReferences.Add( references[i] );
			}

			listView.UpdateList();
		}

		void IListViewAdapter.SetItemContent( RecycledListItem item )
		{
			ObjectReferencePickerItem it = (ObjectReferencePickerItem) item;
			it.SetContent( filteredReferences[it.Position] );

			if( it.Reference == currentlySelectedObject )
			{
				it.IsSelected = true;
				currentlySelectedItem = it;
			}
			else
				it.IsSelected = false;

			it.Skin = Skin;
		}

		void IListViewAdapter.OnItemClicked( RecycledListItem item )
		{
			if( currentlySelectedItem != null )
				currentlySelectedItem.IsSelected = false;

			currentlySelectedItem = (ObjectReferencePickerItem) item;
			currentlySelectedObject = currentlySelectedItem.Reference;
			currentlySelectedItem.IsSelected = true;

			if( onReferenceChanged != null )
				onReferenceChanged( currentlySelectedItem.Reference );
		}

		public static void DestroyInstance()
		{
			if( m_instance )
			{
				RuntimeInspectorUtils.IgnoredTransformsInHierarchy.Remove( m_instance.transform );

				Destroy( m_instance );
				m_instance = null;
			}
		}
	}
}