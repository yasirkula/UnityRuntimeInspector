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

		public delegate void ReferenceCallback( object reference );
		private ReferenceCallback onReferenceChanged, onSelectionConfirmed;

		public delegate string NameGetter( object reference );
		private NameGetter referenceNameGetter, referenceDisplayNameGetter;

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

		private readonly List<object> references = new List<object>( 64 );
		private readonly List<object> filteredReferences = new List<object>( 64 );

		private object initialValue;

		private object currentlySelectedObject;
		private ObjectReferencePickerItem currentlySelectedItem;

		int IListViewAdapter.Count { get { return filteredReferences.Count; } }
		float IListViewAdapter.ItemHeight { get { return Skin.LineHeight; } }

		protected override void Awake()
		{
			base.Awake();

			listView.SetAdapter( this );
			searchBar.onValueChanged.AddListener( OnSearchTextChanged );

			cancelButton.onClick.AddListener( Cancel );
			okButton.onClick.AddListener( () =>
			{
				try
				{
					if( onSelectionConfirmed != null )
						onSelectionConfirmed( currentlySelectedObject );
				}
				catch( Exception e )
				{
					Debug.LogException( e );
				}

				Close();
			} );

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
			// On new Input System, scroll sensitivity is much higher than legacy Input system
			listView.GetComponent<ScrollRect>().scrollSensitivity *= 0.25f;
#endif
		}

		public void Show( ReferenceCallback onReferenceChanged, ReferenceCallback onSelectionConfirmed, NameGetter referenceNameGetter, NameGetter referenceDisplayNameGetter, object[] references, object initialReference, bool includeNullReference, string title, Canvas referenceCanvas )
		{
			initialValue = initialReference;

			this.onReferenceChanged = onReferenceChanged;
			this.onSelectionConfirmed = onSelectionConfirmed;
			this.referenceNameGetter = referenceNameGetter ?? ( ( reference ) => reference.GetNameWithType() );
			this.referenceDisplayNameGetter = referenceDisplayNameGetter ?? ( ( reference ) => reference.GetNameWithType() );

			if( referenceCanvas && this.referenceCanvas != referenceCanvas )
			{
				this.referenceCanvas = referenceCanvas;

				Canvas canvas = GetComponent<Canvas>();
				canvas.CopyValuesFrom( referenceCanvas );
				canvas.sortingOrder = Mathf.Max( 1000, referenceCanvas.sortingOrder + 100 );
			}

			panel.rectTransform.anchoredPosition = Vector2.zero;
			gameObject.SetActive( true );

			selectPromptText.text = title;
			currentlySelectedObject = initialReference;

			GenerateReferenceItems( references, includeNullReference );

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL
			// On desktop platforms, automatically focus on search field
			// We don't do the same on mobile because immediately showing the on-screen keyboard after presenting the window wouldn't be nice
			searchBar.ActivateInputField();
#endif
		}

		public void Cancel()
		{
			try
			{
				if( currentlySelectedObject != initialValue && onReferenceChanged != null )
					onReferenceChanged( initialValue );
			}
			catch( Exception e )
			{
				Debug.LogException( e );
			}

			Close();
		}

		public void Close()
		{
			onReferenceChanged = null;
			onSelectionConfirmed = null;
			referenceNameGetter = null;
			referenceDisplayNameGetter = null;
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

		private void GenerateReferenceItems( object[] references, bool includeNullReference )
		{
			this.references.Clear();
			filteredReferences.Clear();
			searchBar.text = string.Empty;

			if( includeNullReference )
				this.references.Add( null );

			Array.Sort( references, ( ref1, ref2 ) => referenceNameGetter( ref1 ).CompareTo( referenceNameGetter( ref2 ) ) );

			for( int i = 0; i < references.Length; i++ )
			{
				Object unityReference = references[i] as Object;
				if( unityReference )
				{
					if( unityReference.hideFlags != HideFlags.None && unityReference.hideFlags != HideFlags.NotEditable &&
						unityReference.hideFlags != HideFlags.HideInHierarchy && unityReference.hideFlags != HideFlags.HideInInspector )
						continue;

					if( ( unityReference is Texture || unityReference is Sprite ) && unityReference.name.StartsWith( SPRITE_ATLAS_PREFIX ) )
						continue;

					this.references.Add( unityReference );
				}
				else if( references[i] != null )
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
				if( referenceNameGetter( references[i] ).ToLowerInvariant().Contains( value ) )
					filteredReferences.Add( references[i] );
			}

			listView.UpdateList();
		}

		void IListViewAdapter.SetItemContent( RecycledListItem item )
		{
			ObjectReferencePickerItem it = (ObjectReferencePickerItem) item;
			it.SetContent( filteredReferences[it.Position], referenceDisplayNameGetter( filteredReferences[it.Position] ) );

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

			try
			{
				if( onReferenceChanged != null )
					onReferenceChanged( currentlySelectedObject );
			}
			catch( Exception e )
			{
				Debug.LogException( e );
			}
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