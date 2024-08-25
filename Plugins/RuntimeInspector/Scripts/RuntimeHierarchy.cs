using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

namespace RuntimeInspectorNamespace
{
	public class RuntimeHierarchy : SkinnedWindow, IListViewAdapter, ITooltipManager
	{
		[System.Flags]
		public enum SelectOptions { None = 0, Additive = 1, FocusOnSelection = 2, ForceRevealSelection = 4, DontRaiseEvent = 8 }
		public enum LongPressAction { None = 0, CreateDraggedReferenceItem = 1, ShowMultiSelectionToggles = 2, ShowMultiSelectionTogglesThenCreateDraggedReferenceItem = 3 };

		public delegate void SelectionChangedDelegate( ReadOnlyCollection<Transform> selection );
		public delegate void DoubleClickDelegate( HierarchyData clickedItem );
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

		[Space]
		[SerializeField]
		private bool m_allowMultiSelection = true;
		public bool AllowMultiSelection
		{
			get { return m_allowMultiSelection; }
			set
			{
				if( m_allowMultiSelection != value )
				{
					m_allowMultiSelection = value;

					if( !value )
					{
						MultiSelectionToggleSelectionMode = false;

						if( m_currentSelection.Count > 1 )
						{
							for( int i = m_currentSelection.Count - 1; i >= 0; i-- )
							{
								if( m_currentSelection[i] )
								{
									singleTransformSelection[0] = m_currentSelection[i];
									SelectInternal( singleTransformSelection );

									return;
								}
							}

							DeselectInternal( null );
						}
					}
				}
			}
		}

		private bool m_multiSelectionToggleSelectionMode = false;
		private bool justActivatedMultiSelectionToggleSelectionMode = false;
		public bool MultiSelectionToggleSelectionMode
		{
			get { return m_multiSelectionToggleSelectionMode; }
			set
			{
				if( !m_allowMultiSelection )
					value = false;

				if( m_multiSelectionToggleSelectionMode != value )
				{
					m_multiSelectionToggleSelectionMode = value;
					shouldRecalculateContentWidth = true;

					for( int i = drawers.Count - 1; i >= 0; i-- )
					{
						if( drawers[i].gameObject.activeSelf )
							drawers[i].MultiSelectionToggleVisible = value;
					}

					deselectAllButton.gameObject.SetActive( value );

					if( !value )
						EnsureScrollViewIsWithinBounds();
				}
			}
		}

		[Space]
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

		[SerializeField, UnityEngine.Serialization.FormerlySerializedAs( "exposedScenes" )]
		private string[] exposedUnityScenesSubset;

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

		[Space]
		[SerializeField]
		private LongPressAction m_pointerLongPressAction = LongPressAction.CreateDraggedReferenceItem;
		public LongPressAction PointerLongPressAction
		{
			get { return m_pointerLongPressAction; }
			set { m_pointerLongPressAction = value; }
		}

		[SerializeField, UnityEngine.Serialization.FormerlySerializedAs( "m_draggedReferenceHoldTime" )]
		private float m_pointerLongPressDuration = 0.4f;
		public float PointerLongPressDuration
		{
			get { return m_pointerLongPressDuration; }
			set { m_pointerLongPressDuration = value; }
		}

		[SerializeField]
		private float m_doubleClickThreshold = 0.5f;
		public float DoubleClickThreshold
		{
			get { return m_doubleClickThreshold; }
			set { m_doubleClickThreshold = value; }
		}

		[Space]
		[SerializeField]
		private bool m_canReorganizeItems = false;
		public bool CanReorganizeItems
		{
			get { return m_canReorganizeItems; }
			set { m_canReorganizeItems = value; }
		}

		[SerializeField]
		private bool m_canDropDraggedParentOnChild = false;
		public bool CanDropDraggedParentOnChild
		{
			get { return m_canDropDraggedParentOnChild; }
			set { m_canDropDraggedParentOnChild = value; }
		}

		[SerializeField]
		private bool m_canDropDraggedObjectsToPseudoScenes = false;
		public bool CanDropDraggedObjectsToPseudoScenes
		{
			get { return m_canDropDraggedObjectsToPseudoScenes; }
			set { m_canDropDraggedObjectsToPseudoScenes = value; }
		}

		[Space]
		[SerializeField]
		private bool m_showTooltips;
		public bool ShowTooltips { get { return m_showTooltips; } }

		[SerializeField]
		private float m_tooltipDelay = 0.5f;
		public float TooltipDelay
		{
			get { return m_tooltipDelay; }
			set { m_tooltipDelay = value; }
		}

		internal TooltipListener TooltipListener { get; private set; }

		[Space]
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

					for( int i = m_currentSelection.Count - 1; i >= 0; i-- )
					{
						if( m_currentSelection[i] )
						{
							m_connectedInspector.Inspect( m_currentSelection[i].gameObject );
							break;
						}
					}
				}
			}
		}

		private bool m_isLocked = false;
		public bool IsLocked
		{
			get { return m_isLocked; }
			set { m_isLocked = value; }
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
		private Button deselectAllButton;

		[SerializeField]
		private LayoutElement deselectAllLayoutElement;

		[SerializeField]
		private Text deselectAllLabel;

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

		private bool initialized = false;

		private readonly List<HierarchyField> drawers = new List<HierarchyField>( 32 );

		private readonly List<HierarchyDataRoot> sceneData = new List<HierarchyDataRoot>( 8 );
		private readonly List<HierarchyDataRoot> searchSceneData = new List<HierarchyDataRoot>( 8 );
		private readonly Dictionary<string, HierarchyDataRootPseudoScene> pseudoSceneDataLookup = new Dictionary<string, HierarchyDataRootPseudoScene>();

		private readonly List<Transform> m_currentSelection = new List<Transform>( 16 );
		public ReadOnlyCollection<Transform> CurrentSelection { get { return m_currentSelection.AsReadOnly(); } }

		// Stores the selected Transforms' instanceIDs. An object's instanceID can be retrieved via GetInstanceID() but it
		// makes an unnecessary "EnsureRunningOnMainThread()" call. Luckily, GetHashCode() also returns the instanceID and
		// it doesn't call "EnsureRunningOnMainThread()"
		private readonly HashSet<int> currentSelectionSet = new HashSet<int>();
		private readonly HashSet<int> newSelectionSet = new HashSet<int>();

#pragma warning disable 0414 // Value is assigned but never used on Android & iOS
		private Transform multiSelectionPivotTransform;
		private HierarchyDataRoot multiSelectionPivotSceneData;
		private readonly List<int> multiSelectionPivotSiblingIndexTraversalList = new List<int>( 8 );
#pragma warning restore 0414

		private readonly Transform[] singleTransformSelection = new Transform[1];

		private int totalItemCount;
		internal int ItemCount { get { return totalItemCount; } }

		private bool selectLock = false;
		private bool isListViewDirty = true;
		private bool shouldRecalculateContentWidth;

		private float lastClickTime;
		private HierarchyField lastClickedDrawer;
		private HierarchyField currentlyPressedDrawer;
		private float pressedDrawerDraggedReferenceCreateTime;
		private PointerEventData pressedDrawerActivePointer;

		private Canvas m_canvas;
		public Canvas Canvas { get { return m_canvas; } }

		internal float AutoScrollSpeed;

		// Used to make sure that the scrolled content always remains within the scroll view's boundaries
		private PointerEventData nullPointerEventData;

		public SelectionChangedDelegate OnSelectionChanged;
		public DoubleClickDelegate OnItemDoubleClicked;

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
			Initialize();
		}

		private void Initialize()
		{
			if( initialized )
				return;

			initialized = true;

			listView.SetAdapter( this );

			aliveHierarchies++;

			m_canvas = GetComponentInParent<Canvas>();
			nullPointerEventData = new PointerEventData( null );

			searchInputField.onValueChanged.AddListener( OnSearchTermChanged );
			deselectAllButton.onClick.AddListener( () =>
			{
				DeselectInternal( null );
				MultiSelectionToggleSelectionMode = false;
			} );

			m_showHorizontalScrollbar = !m_showHorizontalScrollbar;
			ShowHorizontalScrollbar = !m_showHorizontalScrollbar;

			if( m_showTooltips )
			{
				TooltipListener = gameObject.AddComponent<TooltipListener>();
				TooltipListener.Initialize( this );
			}

			RuntimeInspectorUtils.IgnoredTransformsInHierarchy.Add( drawArea );

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
			// On new Input System, scroll sensitivity is much higher than legacy Input system
			scrollView.scrollSensitivity *= 0.25f;
#endif
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

			Transform[] selection = UnityEditor.Selection.GetFiltered<Transform>( UnityEditor.SelectionMode.ExcludePrefab );
			if( selection.Length > 0 )
				Select( selection );
		}
#endif

		protected override void Update()
		{
			base.Update();

			float time = Time.realtimeSinceStartup;
			if( time > nextHierarchyRefreshTime )
				Refresh();
			if( m_isInSearchMode && time > nextSearchRefreshTime )
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

				if( m_multiSelectionToggleSelectionMode && drawers.Count > 0 )
					preferredWidth += Skin.LineHeight;

				float contentMinWidth = listView.ViewportWidth + scrollView.verticalScrollbarSpacing;
				if( preferredWidth > contentMinWidth )
					scrollView.content.sizeDelta = new Vector2( preferredWidth - contentMinWidth, scrollView.content.sizeDelta.y );
				else
					scrollView.content.sizeDelta = new Vector2( 0f, scrollView.content.sizeDelta.y );

				EnsureScrollViewIsWithinBounds();
			}

			if( m_pointerLongPressAction != LongPressAction.None && currentlyPressedDrawer && time > pressedDrawerDraggedReferenceCreateTime )
			{
				if( currentlyPressedDrawer.gameObject.activeSelf && currentlyPressedDrawer.Data.BoundTransform )
				{
					if( m_pointerLongPressAction == LongPressAction.CreateDraggedReferenceItem || ( m_pointerLongPressAction == LongPressAction.ShowMultiSelectionTogglesThenCreateDraggedReferenceItem && ( !m_allowMultiSelection || m_multiSelectionToggleSelectionMode ) ) )
					{
						Transform[] draggedReferences = currentlyPressedDrawer.IsSelected ? m_currentSelection.ToArray() : new Transform[1] { currentlyPressedDrawer.Data.BoundTransform };
						if( draggedReferences.Length > 1 )
						{
							// The held drawer's Transform should be the first Transform in the array so that it'll be the Transform that will be received
							// by RuntimeInspector's object reference drawers
							int currentlyPressedDrawerIndex = System.Array.IndexOf( draggedReferences, currentlyPressedDrawer.Data.BoundTransform );
							if( currentlyPressedDrawerIndex > 0 )
							{
								for( int i = currentlyPressedDrawerIndex; i > 0; i-- )
									draggedReferences[i] = draggedReferences[i - 1];

								draggedReferences[0] = currentlyPressedDrawer.Data.BoundTransform;
							}
						}

						if( RuntimeInspectorUtils.CreateDraggedReferenceItem( draggedReferences, pressedDrawerActivePointer, Skin, m_canvas ) )
							( (IPointerEnterHandler) dragDropListener ).OnPointerEnter( pressedDrawerActivePointer );
					}
					else if( m_allowMultiSelection && !m_multiSelectionToggleSelectionMode )
					{
						if( currentSelectionSet.Add( currentlyPressedDrawer.Data.BoundTransform.GetHashCode() ) )
						{
							m_currentSelection.Add( currentlyPressedDrawer.Data.BoundTransform );
							currentlyPressedDrawer.IsSelected = true;

							OnCurrentSelectionChanged();
						}

						MultiSelectionToggleSelectionMode = true;
						justActivatedMultiSelectionToggleSelectionMode = true;

						// Tooltip may accidentally appear while long pressing the drawer, hide the tooltip in that case
						if( TooltipListener )
							TooltipListener.OnDrawerHovered( null, null, false );
					}
				}

				currentlyPressedDrawer = null;
				pressedDrawerActivePointer = null;
			}

			if( AutoScrollSpeed != 0f )
				scrollView.verticalNormalizedPosition = Mathf.Clamp01( scrollView.verticalNormalizedPosition + AutoScrollSpeed * Time.unscaledDeltaTime / totalItemCount );
		}

		public void Refresh()
		{
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

		public void RefreshDelayed()
		{
			nextHierarchyRefreshTime = nextSearchRefreshTime = 0f;
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
			EnsureScrollViewIsWithinBounds();
		}

		internal void SetListViewDirty()
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
			searchIcon.color = Skin.TextColor;
			searchBarLayoutElement.SetHeight( Skin.LineHeight );

			deselectAllLayoutElement.SetHeight( Skin.LineHeight );
			deselectAllButton.targetGraphic.color = Skin.InputFieldInvalidBackgroundColor;
			deselectAllLabel.SetSkinInputFieldText( Skin );

			selectedPathBackground.color = Skin.BackgroundColor.Tint( 0.1f );
			selectedPathText.SetSkinText( Skin );

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

		// Makes sure that scroll view's contents are within scroll view's bounds
		private void EnsureScrollViewIsWithinBounds()
		{
			// When scrollbar is snapped to the very bottom of the scroll view, sometimes OnScroll alone doesn't work
			if( scrollView.verticalNormalizedPosition <= Mathf.Epsilon )
				scrollView.verticalNormalizedPosition = 0.0001f;

			scrollView.OnScroll( nullPointerEventData );
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
				drawer.IsSelected = data.BoundTransform && currentSelectionSet.Contains( data.BoundTransform.GetHashCode() );
				drawer.MultiSelectionToggleVisible = m_multiSelectionToggleSelectionMode;
				drawer.Refresh();

				shouldRecalculateContentWidth = true;
			}
		}

		void IListViewAdapter.OnItemClicked( RecycledListItem item )
		{
			HierarchyField drawer = (HierarchyField) item;
			if( OnItemDoubleClicked != null && drawer == lastClickedDrawer && Time.realtimeSinceStartup - lastClickTime <= m_doubleClickThreshold )
			{
				lastClickTime = 0f;
				OnItemDoubleClicked( lastClickedDrawer.Data );
			}
			else
			{
				lastClickTime = Time.realtimeSinceStartup;
				lastClickedDrawer = drawer;

				bool hasSelectionChanged = false;
				Transform clickedTransform = drawer.Data.BoundTransform;
				int clickedTransformInstanceID = clickedTransform ? clickedTransform.GetHashCode() : -1;

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL || UNITY_WSA || UNITY_WSA_10_0
				// When Shift key is held, all items from the pivot item to the clicked item will be selected
				int multiSelectionPivotIndex;
				if( m_allowMultiSelection && FindMultiSelectionPivotAbsoluteIndex( out multiSelectionPivotIndex ) && RuntimeInspectorUtils.IsShiftKeyHeld() )
				{
					newSelectionSet.Clear();

					if( clickedTransform )
					{
						newSelectionSet.Add( clickedTransformInstanceID );

						if( currentSelectionSet.Add( clickedTransformInstanceID ) )
						{
							m_currentSelection.Add( clickedTransform );
							hasSelectionChanged = true;
						}
					}

					// Add new Transforms to selection
					for( int i = multiSelectionPivotIndex, endIndex = drawer.Position, increment = ( multiSelectionPivotIndex < endIndex ) ? 1 : -1; i != endIndex; i += increment )
					{
						Transform newSelection = GetDataAt( i ).BoundTransform;
						if( !newSelection )
							continue;

						int selectionInstanceID = newSelection.GetHashCode();
						newSelectionSet.Add( selectionInstanceID );

						if( currentSelectionSet.Add( selectionInstanceID ) )
						{
							m_currentSelection.Add( newSelection );
							hasSelectionChanged = true;
						}
					}

					// Remove old Transforms from selection
					for( int i = m_currentSelection.Count - 1; i >= 0; i-- )
					{
						Transform oldSelection = m_currentSelection[i];
						if( !oldSelection )
							continue;

						int selectionInstanceID = oldSelection.GetHashCode();
						if( !newSelectionSet.Contains( selectionInstanceID ) )
						{
							m_currentSelection.RemoveAt( i );
							currentSelectionSet.Remove( selectionInstanceID );

							hasSelectionChanged = true;
						}
					}
				}
				else
#endif
				{
					multiSelectionPivotTransform = clickedTransform;
					multiSelectionPivotSceneData = drawer.Data.Root;
					drawer.Data.GetSiblingIndexTraversalList( multiSelectionPivotSiblingIndexTraversalList );

					// When in toggle selection mode or Control key is held, individual items can be multi-selected
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL || UNITY_WSA || UNITY_WSA_10_0
					if( m_allowMultiSelection && ( m_multiSelectionToggleSelectionMode || RuntimeInspectorUtils.IsCtrlKeyHeld() ) )
#else
					if( m_allowMultiSelection && m_multiSelectionToggleSelectionMode )
#endif
					{
						if( clickedTransform )
						{
							if( currentSelectionSet.Add( clickedTransformInstanceID ) )
								m_currentSelection.Add( clickedTransform );
							else
							{
								m_currentSelection.Remove( clickedTransform );
								currentSelectionSet.Remove( clickedTransformInstanceID );

								if( m_currentSelection.Count == 0 )
									MultiSelectionToggleSelectionMode = false;
							}

							hasSelectionChanged = true;
						}
					}
					else
					{
						if( clickedTransform )
						{
							if( m_currentSelection.Count != 1 || m_currentSelection[0] != clickedTransform )
							{
								m_currentSelection.Clear();
								currentSelectionSet.Clear();

								m_currentSelection.Add( clickedTransform );
								currentSelectionSet.Add( clickedTransformInstanceID );

								hasSelectionChanged = true;
							}
						}
						else if( m_currentSelection.Count > 0 )
						{
							m_currentSelection.Clear();
							currentSelectionSet.Clear();

							hasSelectionChanged = true;
						}
					}
				}

				if( hasSelectionChanged )
				{
					for( int i = drawers.Count - 1; i >= 0; i-- )
					{
						if( drawers[i].gameObject.activeSelf )
						{
							Transform drawerTransform = drawers[i].Data.BoundTransform;
							if( drawerTransform )
							{
								if( drawers[i].IsSelected != currentSelectionSet.Contains( drawerTransform.GetHashCode() ) )
									drawers[i].IsSelected = !drawers[i].IsSelected;
							}
							else if( drawers[i].IsSelected )
								drawers[i].IsSelected = false;
						}
					}

					OnCurrentSelectionChanged();
				}

				if( m_isInSearchMode )
				{
					bool shouldShowSearchPathText = false;
					for( int i = m_currentSelection.Count - 1; i >= 0; i-- )
					{
						Transform selection = m_currentSelection[i];
						if( selection )
						{
							// Fetch the object's path and show it in Hierarchy
							System.Text.StringBuilder sb = RuntimeInspectorUtils.stringBuilder;
							sb.Length = 0;
							sb.Append( "Path:" );

							Transform rootTransform = ( (HierarchyDataRootSearch) drawer.Data.Root ).RootTransform;
							while( selection != rootTransform )
							{
								sb.AppendLine().Append( "  " ).Append( selection.name );
								selection = selection.parent;
							}

							selectedPathText.text = sb.ToString();
							shouldShowSearchPathText = true;

							break;
						}
					}

					if( selectedPathBackground.gameObject.activeSelf != shouldShowSearchPathText )
						selectedPathBackground.gameObject.SetActive( shouldShowSearchPathText );
				}
			}
		}

		private bool FindMultiSelectionPivotAbsoluteIndex( out int pivotAbsoluteIndex )
		{
			pivotAbsoluteIndex = 0;

			if( multiSelectionPivotSceneData == null )
				return false;

			bool pivotSceneDataExists = false;
			List<HierarchyDataRoot> sceneData = m_isInSearchMode ? searchSceneData : this.sceneData;
			for( int i = 0; i < sceneData.Count; i++ )
			{
				if( sceneData[i] != multiSelectionPivotSceneData )
					pivotAbsoluteIndex += sceneData[i].Height;
				else
				{
					pivotSceneDataExists = true;
					break;
				}
			}

			if( !pivotSceneDataExists )
				return false;

			if( multiSelectionPivotSiblingIndexTraversalList.Count == 0 )
				return true;

			if( !multiSelectionPivotTransform )
				return false;

			// To find the pivot Transform, first try traversing its recorded sibling indices in the HierarchyDataRoot that contains it
			HierarchyData multiSelectionPivotData = multiSelectionPivotSceneData.TraverseSiblingIndexList( multiSelectionPivotSiblingIndexTraversalList );
			if( multiSelectionPivotData != null && multiSelectionPivotData.BoundTransform == multiSelectionPivotTransform )
			{
				pivotAbsoluteIndex += multiSelectionPivotData.AbsoluteIndex;
				return true;
			}

			// Either HierarchyDataRoot no longer contains the pivot Transform, or the pivot Transform's sibling index have changed. Try finding
			// the pivot Transform among all visible children of the HierarchyDataRoot (if it's a pseudo-scene in which a Transform can appear
			// more than once, try finding the pivot Transform at the same depth as it was before)
			multiSelectionPivotData = multiSelectionPivotSceneData.FindTransformInVisibleChildren( multiSelectionPivotTransform, ( multiSelectionPivotSceneData is HierarchyDataRootPseudoScene ) ? multiSelectionPivotSiblingIndexTraversalList.Count : -1 );
			if( multiSelectionPivotData != null )
			{
				pivotAbsoluteIndex += multiSelectionPivotData.AbsoluteIndex;
				return true;
			}

			// Try finding the pivot Transform at all among all visible children of the pseudo-scene
			if( multiSelectionPivotSceneData is HierarchyDataRootPseudoScene )
			{
				multiSelectionPivotData = multiSelectionPivotSceneData.FindTransformInVisibleChildren( multiSelectionPivotTransform, -1 );
				if( multiSelectionPivotData != null )
				{
					pivotAbsoluteIndex += multiSelectionPivotData.AbsoluteIndex;
					return true;
				}
			}

			return false;
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

				// We have activated MultiSelectionToggleSelectionMode with this press; processing the click would result in
				// deselecting the Transform that we've just selected with this press because its selected state would be toggled
				// again inside OnItemClicked. Simply ignore the click to avoid this issue
				if( justActivatedMultiSelectionToggleSelectionMode )
				{
					justActivatedMultiSelectionToggleSelectionMode = false;
					eventData.eligibleForClick = false;
				}

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
				// On new Input System, DraggedReferenceItems aren't tracked by the PointerEventDatas that initiated them. However, when a DraggedReferenceItem is
				// created by holding a HierarchyField, the PointerEventData's dragged object will be set as the RuntimeHierarchy's ScrollRect. When it happens,
				// trying to scroll the RuntimeHierarchy by holding the DraggedReferenceItem at top/bottom edge of the ScrollRect doesn't work because scrollbar's
				// value is overwritten by the original PointerEventData. We can prevent this issue by stopping original PointerEventData's drag operation here
				if( eventData.dragging && eventData.pointerDrag == scrollView.gameObject && DraggedReferenceItem.InstanceItem )
				{
					eventData.dragging = false;
					eventData.pointerDrag = null;
				}
#endif
			}
			else if( m_pointerLongPressAction != LongPressAction.None )
			{
				currentlyPressedDrawer = drawer;
				pressedDrawerActivePointer = eventData;
				pressedDrawerDraggedReferenceCreateTime = Time.realtimeSinceStartup + m_pointerLongPressDuration;
			}
		}

		public bool Select( Transform selection, SelectOptions selectOptions = SelectOptions.None )
		{
			singleTransformSelection[0] = selection;
			return Select( singleTransformSelection, selectOptions );
		}

		public bool Select( IList<Transform> selection, SelectOptions selectOptions = SelectOptions.None )
		{
			return !m_isLocked && SelectInternal( selection, selectOptions );
		}

		internal bool SelectInternal( IList<Transform> selection, SelectOptions selectOptions = SelectOptions.None )
		{
			if( selectLock )
				return false;

			if( selection.IsEmpty() )
			{
				DeselectInternal( null );
				return true;
			}

			Initialize();

			// Remove null Transforms from existing selection
			for( int i = m_currentSelection.Count - 1; i >= 0; i-- )
			{
				if( m_currentSelection[i] == null )
				{
					currentSelectionSet.Remove( m_currentSelection[i].GetHashCode() );
					m_currentSelection.RemoveAt( i );
				}
			}

			// Make sure that the contents of the hierarchy are up-to-date
			Refresh();
			RefreshSearchResults();

			bool additive = ( selectOptions & SelectOptions.Additive ) == SelectOptions.Additive;
			if( !m_allowMultiSelection )
			{
				additive = false;

				if( selection.Count > 1 )
				{
					// Assertion: At least one Transform isn't null because "selection.IsEmpty()" was false
					for( int i = selection.Count - 1; i >= 0; i-- )
					{
						if( CanSelectTransform( selection[i] ) )
						{
							singleTransformSelection[0] = selection[i];
							selection = singleTransformSelection;

							break;
						}
					}
				}
			}

			bool hasSelectionChanged = false;
			if( additive )
			{
				for( int i = 0; i < selection.Count; i++ )
				{
					Transform _selection = selection[i];
					if( CanSelectTransform( _selection ) && currentSelectionSet.Add( _selection.GetHashCode() ) )
					{
						m_currentSelection.Add( _selection );
						hasSelectionChanged = true;
					}
				}
			}
			else
			{
				newSelectionSet.Clear();

				// Add new Transforms to selection
				for( int i = 0; i < selection.Count; i++ )
				{
					Transform newSelection = selection[i];
					if( !CanSelectTransform( newSelection ) )
						continue;

					int selectionInstanceID = newSelection.GetHashCode();
					newSelectionSet.Add( selectionInstanceID );

					if( currentSelectionSet.Add( selectionInstanceID ) )
					{
						m_currentSelection.Add( newSelection );
						hasSelectionChanged = true;
					}
				}

				// Remove old Transforms from selection
				for( int i = m_currentSelection.Count - 1; i >= 0; i-- )
				{
					int selectionInstanceID = m_currentSelection[i].GetHashCode();
					if( !newSelectionSet.Contains( selectionInstanceID ) )
					{
						m_currentSelection.RemoveAt( i );
						currentSelectionSet.Remove( selectionInstanceID );

						hasSelectionChanged = true;
					}
				}
			}

			if( multiSelectionPivotTransform != null && !currentSelectionSet.Contains( multiSelectionPivotTransform.GetHashCode() ) )
			{
				multiSelectionPivotTransform = null;
				multiSelectionPivotSceneData = null;
			}

			// Expand the selected Transforms' parents if they are currently collapsed
			HierarchyDataTransform itemToFocus = null;
			int itemToFocusSceneDataIndex = 0;
			bool forceRevealSelection = ( selectOptions & SelectOptions.ForceRevealSelection ) == SelectOptions.ForceRevealSelection;
			List<HierarchyDataRoot> sceneData = m_isInSearchMode ? searchSceneData : this.sceneData;
			for( int i = 0; i < m_currentSelection.Count; i++ )
			{
				Transform _selection = m_currentSelection[i];
				Scene selectionScene = _selection.gameObject.scene;
				for( int j = 0; j < sceneData.Count; j++ )
				{
					HierarchyDataRoot data = sceneData[j];
					if( m_isInSearchMode || ( data is HierarchyDataRootPseudoScene ) || ( (HierarchyDataRootScene) data ).Scene == selectionScene )
					{
						HierarchyDataTransform selectionItem = forceRevealSelection ? data.FindTransform( _selection ) : data.FindTransformInVisibleChildren( _selection );
						if( selectionItem != null )
						{
							itemToFocus = selectionItem;
							itemToFocusSceneDataIndex = j;

							multiSelectionPivotTransform = _selection;
							multiSelectionPivotSceneData = data;
							selectionItem.GetSiblingIndexTraversalList( multiSelectionPivotSiblingIndexTraversalList );

							// Transform may exist in multiple places (zero or one Unity scene and zero or more pseudo-scene(s)). After expanding the Transform's parent in
							// one of these scenes, we can call it a day. This way, we'd use less CPU-cycles but the Transform's parent in other scene(s) may not be expanded.
							// If performance is important and expanding the Transform's parent in a single scene is OK, then you can uncomment the line below
							//break;
						}
					}
				}
			}

			RefreshListView();

			if( itemToFocus != null )
			{
				// Focus on the latest selected HierarchyItem
				if( ( selectOptions & SelectOptions.FocusOnSelection ) == SelectOptions.FocusOnSelection )
				{
					int itemIndex = itemToFocus.AbsoluteIndex;
					for( int i = 0; i < itemToFocusSceneDataIndex; i++ )
						itemIndex += sceneData[i].Height;

					// This isn't just the drawArea but rather the RuntimeHierarchy itself because in search mode, when SelectedSearchItemPath is visible,
					// it will decrease the scroll view's height which may result in an incorrect viewportHeight value. This is especially obvious
					// when calling Select immediately after exiting the search mode
					LayoutRebuilder.ForceRebuildLayoutImmediate( (RectTransform) transform );

					// Credit: https://gist.github.com/yasirkula/75ca350fb83ddcc1558d33a8ecf1483f
					float drawAreaHeight = drawArea.rect.height;
					float viewportHeight = ( (RectTransform) drawArea.parent ).rect.height;
					if( drawAreaHeight > viewportHeight )
					{
						float focusPoint = ( (float) itemIndex / totalItemCount ) * drawAreaHeight + Skin.LineHeight * 0.5f;
						scrollView.verticalNormalizedPosition = 1f - Mathf.Clamp01( ( focusPoint - viewportHeight * 0.5f ) / ( drawAreaHeight - viewportHeight ) );
					}
				}
			}

			if( hasSelectionChanged )
				OnCurrentSelectionChanged( ( selectOptions & SelectOptions.DontRaiseEvent ) == SelectOptions.DontRaiseEvent );

			return hasSelectionChanged;
		}

		public void Deselect()
		{
			Deselect( (IList<Transform>) null );
		}

		public void Deselect( Transform deselection )
		{
			singleTransformSelection[0] = deselection;
			Deselect( singleTransformSelection );
		}

		public void Deselect( IList<Transform> deselection )
		{
			if( !m_isLocked )
				DeselectInternal( deselection );
		}

		internal void DeselectInternal( IList<Transform> deselection )
		{
			if( selectLock || m_currentSelection.Count == 0 )
				return;

			Initialize();

			bool hasSelectionChanged = false;
			if( deselection == null )
			{
				m_currentSelection.Clear();
				currentSelectionSet.Clear();

				hasSelectionChanged = true;
			}
			else
			{
				for( int i = deselection.Count - 1; i >= 0; i-- )
				{
					Transform deselect = deselection[i];
					if( deselect && currentSelectionSet.Remove( deselect.GetHashCode() ) )
					{
						m_currentSelection.Remove( deselect );
						hasSelectionChanged = true;
					}
				}
			}

			if( hasSelectionChanged )
			{
				for( int i = drawers.Count - 1; i >= 0; i-- )
				{
					if( drawers[i].gameObject.activeSelf && drawers[i].IsSelected )
						drawers[i].IsSelected = false;
				}

				if( multiSelectionPivotTransform != null && !currentSelectionSet.Contains( multiSelectionPivotTransform.GetHashCode() ) )
				{
					multiSelectionPivotTransform = null;
					multiSelectionPivotSceneData = null;
				}

				if( selectedPathBackground.gameObject.activeSelf )
					selectedPathBackground.gameObject.SetActive( false );

				OnCurrentSelectionChanged();
			}
		}

		public bool IsSelected( Transform transform )
		{
			return transform && currentSelectionSet.Contains( transform.GetHashCode() );
		}

		// Check if Transform is hidden from this RuntimeHierarchy
		private bool CanSelectTransform( Transform transform )
		{
			if( !transform )
				return false;

			if( RuntimeInspectorUtils.IgnoredTransformsInHierarchy.Contains( transform ) || ( m_gameObjectDelegate != null && !m_gameObjectDelegate( transform ) ) )
				return false;

			// When Transform passes the above checks, it doesn't necessarily mean that it is visible since one of its parents might also be hidden from this RuntimeHierarchy.
			// We aren't just checking if all parents of the Transform are visible; imagine the scenario where there exist an A/B/C Transform hierarchy in which A is hidden from
			// this RuntimeHierarchy and we're checking if C is visible. Logically, when A is hidden, its grandchild C would also be hidden implicitly. However, if there is a
			// pseudo-scene with either B or C as a root object, then C would be visible in that pseudo-scene because neither B nor C are explicitly hidden from this RuntimeHierarchy
			Transform nearestRoot = null;
			for( int i = 0; i < sceneData.Count; i++ )
			{
				Transform parent = sceneData[i].GetNearestRootOf( transform );
				if( parent && ( !nearestRoot || parent.IsChildOf( nearestRoot ) ) )
					nearestRoot = parent;
			}

			if( !nearestRoot )
				return false;

			if( nearestRoot != transform )
			{
				// Check if B is explicitly hidden from this RuntimeHierarchy in the A/B/C example above
				for( Transform _transform = transform.parent; _transform && _transform != nearestRoot; _transform = _transform.parent )
				{
					if( RuntimeInspectorUtils.IgnoredTransformsInHierarchy.Contains( _transform ) || ( m_gameObjectDelegate != null && !m_gameObjectDelegate( _transform ) ) )
						return false;
				}
			}

			return true;
		}

		private void OnCurrentSelectionChanged( bool dontRaiseEvent = false )
		{
			selectLock = true;
			try
			{
#if UNITY_EDITOR
				if( syncSelectionWithEditorHierarchy )
				{
					List<GameObject> selection = new List<GameObject>( m_currentSelection.Count );
					for( int i = 0; i < m_currentSelection.Count; i++ )
					{
						if( m_currentSelection[i] )
							selection.Add( m_currentSelection[i].gameObject );
					}

					UnityEditor.Selection.objects = selection.ToArray();
				}
#endif

				if( m_connectedInspector )
				{
					Transform newInspectedObject = m_currentSelection.FindLast( ( transform ) => transform != null );
					m_connectedInspector.Inspect( ( newInspectedObject != null ) ? newInspectedObject.gameObject : null );
				}

				if( OnSelectionChanged != null && !dontRaiseEvent )
					OnSelectionChanged( m_currentSelection.AsReadOnly() );
			}
			catch( System.Exception e )
			{
				Debug.LogException( e );
			}
			finally
			{
				selectLock = false;
			}
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

					// Focus on currently selected object(s) after exiting search mode
					if( m_currentSelection.Count > 0 )
						SelectInternal( m_currentSelection, SelectOptions.FocusOnSelection | SelectOptions.ForceRevealSelection );
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
			if( !ExposeUnityScenes || ( arg0.buildIndex >= 0 && exposedUnityScenesSubset != null && exposedUnityScenesSubset.Length > 0 && System.Array.IndexOf( exposedUnityScenesSubset, arg0.name ) == -1 ) )
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
				return CreatePseudoSceneInternal( scene, null );

			return null;
		}

		public void CreatePseudoScene( string scene, Transform rootTransform = null )
		{
			if( pseudoSceneDataLookup.ContainsKey( scene ) )
				return;

			CreatePseudoSceneInternal( scene, rootTransform );
		}

		private HierarchyDataRootPseudoScene CreatePseudoSceneInternal( string scene, Transform rootTransform )
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

			HierarchyDataRootPseudoScene data = new HierarchyDataRootPseudoScene( this, scene, rootTransform );

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