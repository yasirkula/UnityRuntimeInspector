using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RuntimeInspectorNamespace
{
	public class RuntimeHierarchy : SkinnedWindow, IListViewAdapter
	{
		public delegate void SelectionChangedDelegate( Transform selection );
		public delegate void DoubleClickDelegate( Transform selection );
		public delegate bool GameObjectFilterDelegate( Transform transform );

#pragma warning disable 0649
		[SerializeField]
		private float m_refreshInterval = 0f;
		public float RefreshInterval
		{
			get { return m_refreshInterval; }
			set { m_refreshInterval = value; }
		}

		[SerializeField]
		private float m_objectNamesRefreshInterval = 10f;
		public float ObjectNamesRefreshInterval
		{
			get { return m_objectNamesRefreshInterval; }
			set { m_objectNamesRefreshInterval = value; }
		}

		[SerializeField]
		private float m_searchRefreshInterval = 5f;
		public float SearchRefreshInterval
		{
			get { return m_searchRefreshInterval; }
			set { m_searchRefreshInterval = value; }
		}

		private float nextHierarchyRefreshTime = -1f;
		private float nextObjectNamesRefreshTime = -1f;
		private float nextSearchRefreshTime = -1f;

		[SerializeField]
		private bool m_exposeUnityScenes = true;
		public bool ExposeUnityScenes
		{
			get { return m_exposeUnityScenes; }
			set
			{
				if( m_exposeUnityScenes != value )
				{
					m_exposeUnityScenes = value;

					for( int i = 0; i < SceneManager.sceneCount; i++ )
					{
						if( value )
							OnSceneLoaded( SceneManager.GetSceneAt( i ), LoadSceneMode.Single );
						else
							OnSceneUnloaded( SceneManager.GetSceneAt( i ) );
					}
				}
			}
		}

		[SerializeField]
		private bool m_exposeDontDestroyOnLoadScene = true;
		public bool ExposeDontDestroyOnLoadScene
		{
			get { return m_exposeDontDestroyOnLoadScene; }
			set
			{
				if( m_exposeDontDestroyOnLoadScene != value )
				{
					m_exposeDontDestroyOnLoadScene = value;

					if( value )
						OnSceneLoaded( GetDontDestroyOnLoadScene(), LoadSceneMode.Single );
					else
						OnSceneUnloaded( GetDontDestroyOnLoadScene() );
				}
			}
		}

		[SerializeField]
		private string[] pseudoScenesOrder;

		[SerializeField]
		private bool m_createDraggedReferenceOnHold = true;
		public bool CreateDraggedReferenceOnHold
		{
			get { return m_createDraggedReferenceOnHold; }
			set { m_createDraggedReferenceOnHold = value; }
		}

		[SerializeField]
		private float m_draggedReferenceHoldTime = 0.4f;
		public float DraggedReferenceHoldTime
		{
			get { return m_draggedReferenceHoldTime; }
			set { m_draggedReferenceHoldTime = value; }
		}

		[SerializeField]
		private bool m_canReorganizeItems = false;
		public bool CanReorganizeItems
		{
			get { return m_canReorganizeItems; }
			set { m_canReorganizeItems = value; }
		}

		[SerializeField]
		private float m_doubleClickThreshold = 0.5f;
		public float DoubleClickThreshold
		{
			get { return m_doubleClickThreshold; }
			set { m_doubleClickThreshold = value; }
		}

		[SerializeField]
		private bool m_showHorizontalScrollbar;
		public bool ShowHorizontalScrollbar
		{
			get { return m_showHorizontalScrollbar; }
			set
			{
				if( m_showHorizontalScrollbar != value )
				{
					m_showHorizontalScrollbar = value;

					if( !value )
					{
						scrollView.content.sizeDelta = new Vector2( 0f, scrollView.content.sizeDelta.y );
						scrollView.horizontalNormalizedPosition = 0f;
					}
					else
					{
						for( int i = drawers.Count - 1; i >= 0; i-- )
						{
							if( drawers[i].gameObject.activeSelf )
								drawers[i].RefreshName();
						}

						shouldRecalculateContentWidth = true;
					}

					scrollView.horizontal = value;
				}
			}
		}

		public string SearchTerm
		{
			get { return searchInputField.text; }
			set { searchInputField.text = value; }
		}

		private bool m_isInSearchMode = false;
		public bool IsInSearchMode { get { return m_isInSearchMode; } }

#if UNITY_EDITOR
		[SerializeField]
		private bool syncSelectionWithEditorHierarchy = false;
#endif

		[SerializeField]
		private RuntimeInspector m_connectedInspector;
		public RuntimeInspector ConnectedInspector
		{
			get { return m_connectedInspector; }
			set
			{
				if( m_connectedInspector != value )
				{
					m_connectedInspector = value;
					if( m_currentSelection )
						m_connectedInspector.Inspect( m_currentSelection.gameObject );
				}
			}
		}

		[Header( "Internal Variables" )]
		[SerializeField]
		private ScrollRect scrollView;

		[SerializeField]
		private RectTransform drawArea;

		[SerializeField]
		private RecycledListView listView;

		[SerializeField]
		private Image background;

		[SerializeField]
		private Image verticalScrollbar;

		[SerializeField]
		private Image horizontalScrollbar;

		[SerializeField]
		private InputField searchInputField;

		[SerializeField]
		private Image searchIcon;

		[SerializeField]
		private Image searchInputFieldBackground;

		[SerializeField]
		private LayoutElement searchBarLayoutElement;

		[SerializeField]
		private Image selectedPathBackground;

		[SerializeField]
		private Text selectedPathText;

		[SerializeField]
		private HierarchyDragDropListener dragDropListener;

		[SerializeField]
		private HierarchyField drawerPrefab;

		[SerializeField]
		private Sprite m_sceneDrawerBackground;
		internal Sprite SceneDrawerBackground { get { return m_sceneDrawerBackground; } }

		[SerializeField]
		private Sprite m_transformDrawerBackground;
		internal Sprite TransformDrawerBackground { get { return m_transformDrawerBackground; } }
#pragma warning restore 0649

		private static int aliveHierarchies = 0;

		private readonly List<HierarchyField> drawers = new List<HierarchyField>( 32 );

		private readonly List<HierarchyDataRoot> sceneData = new List<HierarchyDataRoot>( 8 );
		private readonly List<HierarchyDataRoot> searchSceneData = new List<HierarchyDataRoot>( 8 );
		private readonly Dictionary<string, HierarchyDataRootPseudoScene> pseudoSceneDataLookup = new Dictionary<string, HierarchyDataRootPseudoScene>();

		private int totalItemCount;
		internal int ItemCount { get { return totalItemCount; } }

		private bool isListViewDirty = true;
		private bool shouldRecalculateContentWidth;

		private float lastClickTime;
		private HierarchyField currentlyPressedDrawer;
		private float pressedDrawerDraggedReferenceCreateTime;
		private PointerEventData pressedDrawerActivePointer;

		private Canvas m_canvas;
		public Canvas Canvas { get { return m_canvas; } }

		private float m_autoScrollSpeed;
		internal float AutoScrollSpeed { set { m_autoScrollSpeed = value; } }

		// Used to make sure that the scrolled content always remains within the scroll view's boundaries
		private PointerEventData nullPointerEventData;

		public SelectionChangedDelegate OnSelectionChanged;
		public DoubleClickDelegate OnItemDoubleClicked;

		private Transform m_currentSelection = null;
		public Transform CurrentSelection
		{
			get { return m_currentSelection; }
			private set
			{
				if( !value )
					value = null;

				if( m_currentSelection != value )
				{
					m_currentSelection = value;

#if UNITY_EDITOR
					if( syncSelectionWithEditorHierarchy )
						UnityEditor.Selection.activeTransform = m_currentSelection;
#endif

					if( OnSelectionChanged != null )
						OnSelectionChanged( m_currentSelection );
				}
			}
		}

		private GameObjectFilterDelegate m_gameObjectDelegate;
		public GameObjectFilterDelegate GameObjectFilter
		{
			get { return m_gameObjectDelegate; }
			set
			{
				m_gameObjectDelegate = value;

				for( int i = 0; i < sceneData.Count; i++ )
				{
					if( sceneData[i].IsExpanded )
					{
						sceneData[i].IsExpanded = false;
						sceneData[i].IsExpanded = true;
					}
				}

				if( m_isInSearchMode )
				{
					for( int i = 0; i < searchSceneData.Count; i++ )
					{
						if( searchSceneData[i].IsExpanded )
						{
							searchSceneData[i].IsExpanded = false;
							searchSceneData[i].IsExpanded = true;
						}
					}
				}
			}
		}

		int IListViewAdapter.Count { get { return totalItemCount; } }
		float IListViewAdapter.ItemHeight { get { return Skin.LineHeight; } }

		protected override void Awake()
		{
			base.Awake();
			listView.SetAdapter( this );

			aliveHierarchies++;

			m_canvas = GetComponentInParent<Canvas>();
			nullPointerEventData = new PointerEventData( null );

			searchInputField.onValueChanged.AddListener( OnSearchTermChanged );
			OnSelectionChanged += ( transform ) =>
			{
				if( ConnectedInspector )
				{
					if( !transform )
						ConnectedInspector.StopInspect();
					else
						ConnectedInspector.Inspect( transform.gameObject );
				}
			};

			m_showHorizontalScrollbar = !m_showHorizontalScrollbar;
			ShowHorizontalScrollbar = !m_showHorizontalScrollbar;

			RuntimeInspectorUtils.IgnoredTransformsInHierarchy.Add( drawArea );
		}

		private void Start()
		{
			SceneManager.sceneLoaded += OnSceneLoaded;
			SceneManager.sceneUnloaded += OnSceneUnloaded;

			if( ExposeUnityScenes )
			{
				for( int i = 0; i < SceneManager.sceneCount; i++ )
					OnSceneLoaded( SceneManager.GetSceneAt( i ), LoadSceneMode.Single );
			}

			if( ExposeDontDestroyOnLoadScene )
				OnSceneLoaded( GetDontDestroyOnLoadScene(), LoadSceneMode.Single );
		}

		private void OnDestroy()
		{
			SceneManager.sceneLoaded -= OnSceneLoaded;
			SceneManager.sceneUnloaded -= OnSceneUnloaded;

			if( --aliveHierarchies == 0 )
				HierarchyData.ClearPool();

			RuntimeInspectorUtils.IgnoredTransformsInHierarchy.Remove( drawArea );
		}

		private void OnRectTransformDimensionsChange()
		{
			shouldRecalculateContentWidth = true;
		}

		private void OnTransformParentChanged()
		{
			m_canvas = GetComponentInParent<Canvas>();
		}

#if UNITY_EDITOR
		private void OnEnable()
		{
			UnityEditor.Selection.selectionChanged -= OnEditorSelectionChanged;
			UnityEditor.Selection.selectionChanged += OnEditorSelectionChanged;
		}

		private void OnDisable()
		{
			UnityEditor.Selection.selectionChanged -= OnEditorSelectionChanged;
		}

		private void OnEditorSelectionChanged()
		{
			if( !syncSelectionWithEditorHierarchy )
				return;

			if( UnityEditor.Selection.activeTransform || !UnityEditor.Selection.activeObject )
				Select( UnityEditor.Selection.activeTransform );
		}
#endif

		protected override void Update()
		{
			base.Update();

			float time = Time.realtimeSinceStartup;
			if( !m_isInSearchMode )
			{
				if( time > nextHierarchyRefreshTime )
					Refresh();
			}
			else if( time > nextSearchRefreshTime )
				RefreshSearchResults();

			if( isListViewDirty )
				RefreshListView();

			if( time > nextObjectNamesRefreshTime )
			{
				nextObjectNamesRefreshTime = time + m_objectNamesRefreshInterval;

				for( int i = sceneData.Count - 1; i >= 0; i-- )
					sceneData[i].ResetCachedNames();

				for( int i = searchSceneData.Count - 1; i >= 0; i-- )
					searchSceneData[i].ResetCachedNames();

				for( int i = drawers.Count - 1; i >= 0; i-- )
				{
					if( drawers[i].gameObject.activeSelf )
						drawers[i].RefreshName();
				}

				shouldRecalculateContentWidth = true;
			}

			if( m_showHorizontalScrollbar && shouldRecalculateContentWidth )
			{
				shouldRecalculateContentWidth = false;

				float preferredWidth = 0f;
				for( int i = drawers.Count - 1; i >= 0; i-- )
				{
					if( drawers[i].gameObject.activeSelf )
					{
						float drawerWidth = drawers[i].PreferredWidth;
						if( drawerWidth > preferredWidth )
							preferredWidth = drawerWidth;
					}
				}

				float contentMinWidth = listView.ViewportWidth + scrollView.verticalScrollbarSpacing;
				if( preferredWidth > contentMinWidth )
					scrollView.content.sizeDelta = new Vector2( preferredWidth - contentMinWidth, scrollView.content.sizeDelta.y );
				else
					scrollView.content.sizeDelta = new Vector2( 0f, scrollView.content.sizeDelta.y );
			}

			if( m_createDraggedReferenceOnHold && currentlyPressedDrawer && time > pressedDrawerDraggedReferenceCreateTime )
			{
				if( currentlyPressedDrawer.gameObject.activeSelf && currentlyPressedDrawer.Data.BoundTransform )
				{
					if( RuntimeInspectorUtils.CreateDraggedReferenceItem( currentlyPressedDrawer.Data.BoundTransform, pressedDrawerActivePointer, Skin, m_canvas ) )
						( (IPointerEnterHandler) dragDropListener ).OnPointerEnter( pressedDrawerActivePointer );
				}

				currentlyPressedDrawer = null;
				pressedDrawerActivePointer = null;
			}

			if( m_autoScrollSpeed != 0f )
				scrollView.verticalNormalizedPosition = Mathf.Clamp01( scrollView.verticalNormalizedPosition + m_autoScrollSpeed * Time.unscaledDeltaTime / totalItemCount );
		}

		public void Refresh()
		{
			if( m_isInSearchMode )
				return;

			nextHierarchyRefreshTime = Time.realtimeSinceStartup + m_refreshInterval;

			bool hasChanged = false;
			for( int i = 0; i < sceneData.Count; i++ )
				hasChanged |= sceneData[i].Refresh();

			if( hasChanged )
				isListViewDirty = true;
			else
			{
				for( int i = drawers.Count - 1; i >= 0; i-- )
				{
					if( drawers[i].gameObject.activeSelf )
						drawers[i].Refresh();
				}
			}
		}

		private void RefreshListView()
		{
			isListViewDirty = false;

			totalItemCount = 0;
			if( !m_isInSearchMode )
			{
				for( int i = sceneData.Count - 1; i >= 0; i-- )
					totalItemCount += sceneData[i].Height;
			}
			else
			{
				for( int i = searchSceneData.Count - 1; i >= 0; i-- )
					totalItemCount += searchSceneData[i].Height;
			}

			listView.UpdateList( false );
			scrollView.OnScroll( nullPointerEventData );
		}

		public void SetListViewDirty()
		{
			isListViewDirty = true;
		}

		public void RefreshSearchResults()
		{
			if( !m_isInSearchMode )
				return;

			nextSearchRefreshTime = Time.realtimeSinceStartup + m_searchRefreshInterval;

			for( int i = 0; i < searchSceneData.Count; i++ )
			{
				HierarchyDataRootSearch data = (HierarchyDataRootSearch) searchSceneData[i];

				bool wasExpandable = data.CanExpand;
				data.Refresh();
				if( data.CanExpand && !wasExpandable )
					data.IsExpanded = true;

				isListViewDirty = true;
			}
		}

		public void RefreshNameOf( Transform target )
		{
			if( target )
			{
				Scene targetScene = target.gameObject.scene;
				for( int i = sceneData.Count - 1; i >= 0; i-- )
				{
					HierarchyDataRoot data = sceneData[i];
					if( ( data is HierarchyDataRootPseudoScene ) || ( (HierarchyDataRootScene) data ).Scene == targetScene )
						sceneData[i].RefreshNameOf( target );
				}

				if( m_isInSearchMode )
				{
					RefreshSearchResults();

					for( int i = searchSceneData.Count - 1; i >= 0; i-- )
						searchSceneData[i].RefreshNameOf( target );
				}

				for( int i = drawers.Count - 1; i >= 0; i-- )
				{
					if( drawers[i].gameObject.activeSelf && drawers[i].Data.BoundTransform == target )
						drawers[i].RefreshName();
				}

				shouldRecalculateContentWidth = true;
			}
		}

		protected override void RefreshSkin()
		{
			background.color = Skin.BackgroundColor;
			verticalScrollbar.color = Skin.ScrollbarColor;
			horizontalScrollbar.color = Skin.ScrollbarColor;

			searchInputField.textComponent.SetSkinInputFieldText( Skin );
			searchInputFieldBackground.color = Skin.InputFieldNormalBackgroundColor.Tint( 0.08f );
			searchIcon.color = Skin.ButtonTextColor;
			searchBarLayoutElement.SetHeight( Skin.LineHeight );

			selectedPathBackground.color = Skin.BackgroundColor.Tint( 0.1f );
			selectedPathText.SetSkinButtonText( Skin );

			Text placeholder = searchInputField.placeholder as Text;
			if( placeholder != null )
			{
				float placeholderAlpha = placeholder.color.a;
				placeholder.SetSkinInputFieldText( Skin );

				Color placeholderColor = placeholder.color;
				placeholderColor.a = placeholderAlpha;
				placeholder.color = placeholderColor;
			}

			LayoutRebuilder.ForceRebuildLayoutImmediate( drawArea );
			listView.ResetList();
		}

		void IListViewAdapter.SetItemContent( RecycledListItem item )
		{
			if( isListViewDirty )
				RefreshListView();

			HierarchyField drawer = (HierarchyField) item;
			HierarchyData data = GetDataAt( drawer.Position );
			if( data != null )
			{
				drawer.Skin = Skin;
				drawer.SetContent( data );
				drawer.IsSelected = m_currentSelection && m_currentSelection == data.BoundTransform;
				drawer.Refresh();

				shouldRecalculateContentWidth = true;
			}
		}

		void IListViewAdapter.OnItemClicked( RecycledListItem item )
		{
			HierarchyField drawer = (HierarchyField) item;
			if( !drawer )
			{
				if( m_currentSelection )
				{
					for( int i = drawers.Count - 1; i >= 0; i-- )
					{
						if( drawers[i].gameObject.activeSelf && drawers[i].Data.BoundTransform == m_currentSelection )
							drawers[i].IsSelected = false;
					}

					CurrentSelection = null;
				}
			}
			else if( m_currentSelection == drawer.Data.BoundTransform )
			{
				if( OnItemDoubleClicked != null )
				{
					if( Time.realtimeSinceStartup - lastClickTime <= m_doubleClickThreshold )
					{
						lastClickTime = 0f;

						if( m_currentSelection )
							OnItemDoubleClicked( m_currentSelection );
					}
					else
						lastClickTime = Time.realtimeSinceStartup;
				}
			}
			else
			{
				Transform clickedTransform = drawer.Data.BoundTransform;
				for( int i = drawers.Count - 1; i >= 0; i-- )
				{
					if( drawers[i].gameObject.activeSelf )
					{
						Transform drawerTransform = drawers[i].Data.BoundTransform;
						if( drawerTransform == m_currentSelection )
							drawers[i].IsSelected = false;
						else if( drawerTransform == clickedTransform && clickedTransform )
							drawers[i].IsSelected = true;
					}
				}

				lastClickTime = Time.realtimeSinceStartup;
				CurrentSelection = clickedTransform;

				if( m_isInSearchMode && clickedTransform )
				{
					// Fetch the object's path and show it in Hierarchy
					System.Text.StringBuilder sb = RuntimeInspectorUtils.stringBuilder;
					sb.Length = 0;

					sb.AppendLine( "Path:" );
					while( clickedTransform )
					{
						sb.Append( "  " ).AppendLine( clickedTransform.name );
						clickedTransform = clickedTransform.parent;
					}

					selectedPathText.text = sb.Append( "  " ).Append( drawer.Data.Root.Name ).ToString();
					selectedPathBackground.gameObject.SetActive( true );
				}
			}
		}

		internal HierarchyData GetDataAt( int index )
		{
			List<HierarchyDataRoot> rootData = !m_isInSearchMode ? sceneData : searchSceneData;
			for( int i = 0; i < rootData.Count; i++ )
			{
				if( rootData[i].Depth < 0 )
					continue;

				if( index < rootData[i].Height )
					return index > 0 ? rootData[i].FindDataAtIndex( index - 1 ) : rootData[i];
				else
					index -= rootData[i].Height;
			}

			return null;
		}

		public void OnDrawerPointerEvent( HierarchyField drawer, PointerEventData eventData, bool isPointerDown )
		{
			if( !isPointerDown )
			{
				currentlyPressedDrawer = null;
				pressedDrawerActivePointer = null;
			}
			else if( m_createDraggedReferenceOnHold )
			{
				currentlyPressedDrawer = drawer;
				pressedDrawerActivePointer = eventData;
				pressedDrawerDraggedReferenceCreateTime = Time.realtimeSinceStartup + m_draggedReferenceHoldTime;
			}
		}

		public bool Select( Transform selection, bool forceSelection = false )
		{
			if( !selection )
			{
				Deselect();
				return true;
			}
			else
			{
				if( !forceSelection && selection == m_currentSelection )
					return true;

				CurrentSelection = selection;

				// Make sure that the contents of the hierarchy are up-to-date
				Refresh();

				Scene selectionScene = selection.gameObject.scene;
				for( int i = 0; i < sceneData.Count; i++ )
				{
					HierarchyDataRoot data = sceneData[i];
					if( ( data is HierarchyDataRootPseudoScene ) || ( (HierarchyDataRootScene) data ).Scene == selectionScene )
					{
						HierarchyDataTransform selectionItem = sceneData[i].FindTransform( selection );
						if( selectionItem != null )
						{
							RefreshListView();

							// Focus on selected HierarchyItem
							int itemIndex = selectionItem.AbsoluteIndex;
							for( int j = 0; j < i; j++ )
								itemIndex += sceneData[i].Height;

							LayoutRebuilder.ForceRebuildLayoutImmediate( drawArea );
							scrollView.verticalNormalizedPosition = Mathf.Clamp01( 1f - (float) itemIndex / totalItemCount );

							return true;
						}
					}
				}
			}

			return false;
		}

		public void Deselect()
		{
			( (IListViewAdapter) this ).OnItemClicked( null );
		}

		private void OnSearchTermChanged( string search )
		{
			if( search != null )
				search = search.Trim();

			if( string.IsNullOrEmpty( search ) )
			{
				if( m_isInSearchMode )
				{
					for( int i = 0; i < searchSceneData.Count; i++ )
						searchSceneData[i].IsExpanded = false;

					scrollView.verticalNormalizedPosition = 1f;
					selectedPathBackground.gameObject.SetActive( false );

					isListViewDirty = true;
					m_isInSearchMode = false;

					// Focus on currently selected object after exiting search mode
					if( m_currentSelection )
						Select( m_currentSelection, true );
				}
			}
			else
			{
				if( !m_isInSearchMode )
				{
					scrollView.verticalNormalizedPosition = 1f;
					nextSearchRefreshTime = Time.realtimeSinceStartup + m_searchRefreshInterval;

					isListViewDirty = true;
					m_isInSearchMode = true;

					RefreshSearchResults();

					for( int i = 0; i < searchSceneData.Count; i++ )
						searchSceneData[i].IsExpanded = true;
				}
				else
					RefreshSearchResults();
			}
		}

		private void OnSceneLoaded( Scene arg0, LoadSceneMode arg1 )
		{
			if( !ExposeUnityScenes )
				return;

			if( !arg0.IsValid() )
				return;

			for( int i = 0; i < sceneData.Count; i++ )
			{
				if( sceneData[i] is HierarchyDataRootScene && ( (HierarchyDataRootScene) sceneData[i] ).Scene == arg0 )
					return;
			}

			HierarchyDataRootScene data = new HierarchyDataRootScene( this, arg0 );
			data.Refresh();

			// Unity scenes should come before pseudo-scenes
			int index = sceneData.Count - pseudoSceneDataLookup.Count;
			sceneData.Insert( index, data );
			searchSceneData.Insert( index, new HierarchyDataRootSearch( this, data ) );

			isListViewDirty = true;
		}

		private void OnSceneUnloaded( Scene arg0 )
		{
			for( int i = 0; i < sceneData.Count; i++ )
			{
				if( sceneData[i] is HierarchyDataRootScene && ( (HierarchyDataRootScene) sceneData[i] ).Scene == arg0 )
				{
					sceneData[i].IsExpanded = false;
					sceneData.RemoveAt( i );

					searchSceneData[i].IsExpanded = false;
					searchSceneData.RemoveAt( i );

					isListViewDirty = true;
					return;
				}
			}
		}

		private Scene GetDontDestroyOnLoadScene()
		{
			GameObject temp = null;
			try
			{
				temp = new GameObject();
				DontDestroyOnLoad( temp );
				Scene dontDestroyOnLoad = temp.scene;
				DestroyImmediate( temp );
				temp = null;

				return dontDestroyOnLoad;
			}
			catch( System.Exception e )
			{
				Debug.LogException( e );
				return new Scene();
			}
			finally
			{
				if( temp != null )
					DestroyImmediate( temp );
			}
		}

		public void AddToPseudoScene( string scene, Transform transform )
		{
			GetPseudoScene( scene, true ).AddChild( transform );
		}

		public void AddToPseudoScene( string scene, IEnumerable<Transform> transforms )
		{
			HierarchyDataRootPseudoScene pseudoScene = GetPseudoScene( scene, true );
			foreach( Transform transform in transforms )
				pseudoScene.AddChild( transform );
		}

		public void RemoveFromPseudoScene( string scene, Transform transform, bool deleteSceneIfEmpty )
		{
			HierarchyDataRootPseudoScene pseudoScene = GetPseudoScene( scene, false );
			if( pseudoScene == null )
				return;

			pseudoScene.RemoveChild( transform );

			if( deleteSceneIfEmpty && pseudoScene.ChildCount == 0 )
				DeletePseudoScene( scene );
		}

		public void RemoveFromPseudoScene( string scene, IEnumerable<Transform> transforms, bool deleteSceneIfEmpty )
		{
			HierarchyDataRootPseudoScene pseudoScene = GetPseudoScene( scene, false );
			if( pseudoScene == null )
				return;

			foreach( Transform transform in transforms )
				pseudoScene.RemoveChild( transform );

			if( deleteSceneIfEmpty && pseudoScene.ChildCount == 0 )
				DeletePseudoScene( scene );
		}

		private HierarchyDataRootPseudoScene GetPseudoScene( string scene, bool createIfNotExists )
		{
			HierarchyDataRootPseudoScene data;
			if( pseudoSceneDataLookup.TryGetValue( scene, out data ) )
				return data;

			if( createIfNotExists )
				return CreatePseudoSceneInternal( scene );

			return null;
		}

		public void CreatePseudoScene( string scene )
		{
			if( pseudoSceneDataLookup.ContainsKey( scene ) )
				return;

			CreatePseudoSceneInternal( scene );
		}

		private HierarchyDataRootPseudoScene CreatePseudoSceneInternal( string scene )
		{
			int index = 0;
			if( pseudoScenesOrder.Length > 0 )
			{
				for( int i = 0; i < pseudoScenesOrder.Length; i++ )
				{
					if( pseudoScenesOrder[i] == scene )
						break;

					if( pseudoSceneDataLookup.ContainsKey( pseudoScenesOrder[i] ) )
						index++;
				}
			}
			else
				index = pseudoSceneDataLookup.Count;

			HierarchyDataRootPseudoScene data = new HierarchyDataRootPseudoScene( this, scene );

			// Pseudo-scenes should come after Unity scenes
			index += sceneData.Count - pseudoSceneDataLookup.Count;
			sceneData.Insert( index, data );
			searchSceneData.Insert( index, new HierarchyDataRootSearch( this, data ) );
			pseudoSceneDataLookup[scene] = data;

			isListViewDirty = true;
			return data;
		}

		public void DeleteAllPseudoScenes()
		{
			for( int i = sceneData.Count - 1; i >= 0; i-- )
			{
				if( sceneData[i] is HierarchyDataRootPseudoScene )
				{
					sceneData[i].IsExpanded = false;
					sceneData.RemoveAt( i );

					searchSceneData[i].IsExpanded = false;
					searchSceneData.RemoveAt( i );
				}
			}

			pseudoSceneDataLookup.Clear();
			isListViewDirty = true;
		}

		public void DeletePseudoScene( string scene )
		{
			for( int i = 0; i < sceneData.Count; i++ )
			{
				HierarchyDataRootPseudoScene pseudoScene = sceneData[i] as HierarchyDataRootPseudoScene;
				if( pseudoScene != null && pseudoScene.Name == scene )
				{
					pseudoSceneDataLookup.Remove( pseudoScene.Name );

					sceneData[i].IsExpanded = false;
					sceneData.RemoveAt( i );

					searchSceneData[i].IsExpanded = false;
					searchSceneData.RemoveAt( i );

					isListViewDirty = true;
					return;
				}
			}
		}

		RecycledListItem IListViewAdapter.CreateItem( Transform parent )
		{
			HierarchyField result = (HierarchyField) Instantiate( drawerPrefab, parent, false );
			result.Initialize( this );
			result.Skin = Skin;

			drawers.Add( result );
			return result;
		}
	}
}