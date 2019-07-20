using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RuntimeInspectorNamespace
{
	public class RuntimeHierarchy : SkinnedWindow
	{
		private const string POOL_OBJECT_NAME = "RuntimeHierarchyPool";

		public delegate void SelectionChangedDelegate( Transform selection );
		public delegate void DoubleClickDelegate( Transform selection );
		public delegate bool GameObjectFilterDelegate( Transform transform );

#pragma warning disable 0649
		[SerializeField]
		[UnityEngine.Serialization.FormerlySerializedAs( "refreshInterval" )]
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
		private float nextSearchRefreshTime = -1f;

		[SerializeField]
		private int poolCapacity = 64;
		private Transform poolParent;
		private static int aliveHierarchies = 0;
		private static List<HierarchyItem> sceneDrawerPool = new List<HierarchyItem>( 8 );
		private static List<HierarchyItem> transformDrawerPool;
		private static List<HierarchyItem> searchEntryDrawerPool;

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
		[UnityEngine.Serialization.FormerlySerializedAs( "doubleClickThreshold" )]
		private float m_doubleClickThreshold = 0.5f;
		public float DoubleClickThreshold
		{
			get { return m_doubleClickThreshold; }
			set { m_doubleClickThreshold = value; }
		}

		private float lastClickTime;

		private Transform m_currentSelection = null;
		public Transform CurrentSelection
		{
			get { return m_currentSelection; }
			private set
			{
				if( value != null && value.Equals( null ) )
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
					if( CurrentSelection != null )
						m_connectedInspector.Inspect( CurrentSelection.gameObject );
				}
			}
		}

		[Header( "Internal Variables" )]
		[SerializeField]
		private ScrollRect scrollView;

		[SerializeField]
		private RectTransform drawAreaHierarchy;

		[SerializeField]
		private RectTransform drawAreaSearchResults;

		[SerializeField]
		private Image background;

		[SerializeField]
		private Image scrollbar;

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

		private List<HierarchyItemRoot> sceneDrawers = new List<HierarchyItemRoot>( 8 );
		private List<HierarchyItemRoot> searchSceneDrawers = new List<HierarchyItemRoot>( 8 );

		[SerializeField]
		private HierarchyItemRoot sceneDrawerPrefab;

		[SerializeField]
		private HierarchyItemTransform transformDrawerPrefab;

		[SerializeField]
		private HierarchyItemSearchEntry searchEntryDrawerPrefab;
#pragma warning restore 0649

		private HierarchyItem currentlySelectedDrawer = null;

		private Dictionary<string, HierarchyItemRoot> pseudoSceneDrawers = new Dictionary<string, HierarchyItemRoot>();

		public SelectionChangedDelegate OnSelectionChanged;
		public DoubleClickDelegate OnItemDoubleClicked;

		private GameObjectFilterDelegate m_gameObjectDelegate;
		public GameObjectFilterDelegate GameObjectFilter
		{
			get { return m_gameObjectDelegate; }
			set
			{
				m_gameObjectDelegate = value;

				for( int i = 0; i < sceneDrawers.Count; i++ )
				{
					if( sceneDrawers[i].IsExpanded )
					{
						sceneDrawers[i].IsExpanded = false;
						sceneDrawers[i].IsExpanded = true;
					}
				}

				if( m_isInSearchMode )
				{
					for( int i = 0; i < searchSceneDrawers.Count; i++ )
					{
						HierarchyItemRoot sceneDrawer = searchSceneDrawers[i];
						if( sceneDrawer.gameObject.activeSelf && sceneDrawer.IsExpanded )
						{
							sceneDrawer.IsExpanded = false;
							sceneDrawer.IsExpanded = true;
						}
					}
				}
			}
		}

		protected override void Awake()
		{
			base.Awake();

			if( transformDrawerPool == null )
				transformDrawerPool = new List<HierarchyItem>( poolCapacity );
			if( searchEntryDrawerPool == null )
				searchEntryDrawerPool = new List<HierarchyItem>( poolCapacity );

			GameObject poolParentGO = GameObject.Find( POOL_OBJECT_NAME );
			if( poolParentGO == null )
			{
				poolParentGO = new GameObject( POOL_OBJECT_NAME );
				DontDestroyOnLoad( poolParentGO );
			}

			poolParent = poolParentGO.transform;
			aliveHierarchies++;

			OnSelectionChanged += ( transform ) =>
			{
				if( !ConnectedInspector.IsNull() )
				{
					if( transform.IsNull() )
						ConnectedInspector.StopInspect();
					else
						ConnectedInspector.Inspect( transform.gameObject );
				}
			};

			searchInputField.onValueChanged.AddListener( OnSearchTermChanged );

			RuntimeInspectorUtils.IgnoredTransformsInHierarchy.Add( drawAreaHierarchy );
			RuntimeInspectorUtils.IgnoredTransformsInHierarchy.Add( drawAreaSearchResults );
			RuntimeInspectorUtils.IgnoredTransformsInHierarchy.Add( poolParent );
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
			if( --aliveHierarchies == 0 )
			{
				if( !poolParent.IsNull() )
				{
					RuntimeInspectorUtils.IgnoredTransformsInHierarchy.Remove( poolParent );
					DestroyImmediate( poolParent.gameObject );
				}

				sceneDrawerPool.Clear();

				if( transformDrawerPool != null )
					transformDrawerPool.Clear();
				if( searchEntryDrawerPool != null )
					searchEntryDrawerPool.Clear();
			}

			RuntimeInspectorUtils.IgnoredTransformsInHierarchy.Remove( drawAreaHierarchy );
			RuntimeInspectorUtils.IgnoredTransformsInHierarchy.Remove( drawAreaSearchResults );
		}

#if UNITY_EDITOR
		private void OnEnable()
		{
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

			if( UnityEditor.Selection.activeTransform != null || UnityEditor.Selection.activeObject == null )
				Select( UnityEditor.Selection.activeTransform );
		}
#endif

		protected override void Update()
		{
			base.Update();

			if( Time.realtimeSinceStartup > nextHierarchyRefreshTime )
			{
				nextHierarchyRefreshTime = Time.realtimeSinceStartup + m_refreshInterval;
				Refresh();
			}

			if( m_isInSearchMode && Time.realtimeSinceStartup > nextSearchRefreshTime )
			{
				nextSearchRefreshTime = Time.realtimeSinceStartup + m_searchRefreshInterval;
				RefreshSearchResults();
			}
		}

		public void Refresh()
		{
			for( int i = 0; i < sceneDrawers.Count; i++ )
				sceneDrawers[i].Refresh();
		}

		public void RefreshSearchResults()
		{
			if( !m_isInSearchMode )
				return;

			for( int i = 0; i < searchSceneDrawers.Count; i++ )
			{
				HierarchyItemRoot sceneDrawer = searchSceneDrawers[i];
				sceneDrawer.Refresh();

				if( sceneDrawer.Content.Children.Count > 0 )
				{
					if( !sceneDrawer.gameObject.activeSelf )
					{
						sceneDrawer.gameObject.SetActive( true );
						sceneDrawer.IsExpanded = true;
					}
				}
				else if( sceneDrawer.gameObject.activeSelf )
					sceneDrawer.gameObject.SetActive( false );
			}
		}

		public void RefreshNameOf( Transform target )
		{
			if( !target.IsNull() )
			{
				Scene targetScene = target.gameObject.scene;
				for( int i = 0; i < sceneDrawers.Count; i++ )
				{
					IHierarchyRootContent content = sceneDrawers[i].Content;
					if( ( content is HierarchyRootPseudoScene ) || ( (HierarchyRootScene) content ).Scene == targetScene )
						sceneDrawers[i].RefreshNameOf( target );
				}

				if( m_isInSearchMode )
				{
					RefreshSearchResults();

					for( int i = 0; i < searchSceneDrawers.Count; i++ )
						searchSceneDrawers[i].RefreshNameOf( target );
				}
			}
		}

		protected override void RefreshSkin()
		{
			background.color = Skin.BackgroundColor;
			scrollbar.color = Skin.ScrollbarColor;

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

			for( int i = 0; i < sceneDrawers.Count; i++ )
				sceneDrawers[i].Skin = Skin;

			for( int i = 0; i < searchSceneDrawers.Count; i++ )
				searchSceneDrawers[i].Skin = Skin;

			LayoutRebuilder.ForceRebuildLayoutImmediate( drawAreaHierarchy );
			LayoutRebuilder.ForceRebuildLayoutImmediate( drawAreaSearchResults );
		}

		public void OnClicked( HierarchyItem drawer )
		{
			if( currentlySelectedDrawer == drawer )
			{
				if( OnItemDoubleClicked != null )
				{
					if( !drawer.IsNull() && Time.realtimeSinceStartup - lastClickTime <= m_doubleClickThreshold )
					{
						lastClickTime = 0f;
						if( drawer is HierarchyItemTransform )
						{
							Transform target = ( (HierarchyItemTransform) drawer ).BoundTransform;
							if( !target.IsNull() )
								OnItemDoubleClicked( target );
						}
					}
					else
						lastClickTime = Time.realtimeSinceStartup;
				}

				return;
			}

			lastClickTime = Time.realtimeSinceStartup;

			if( !currentlySelectedDrawer.IsNull() )
				currentlySelectedDrawer.IsSelected = false;

			currentlySelectedDrawer = drawer;

			if( !drawer.IsNull() )
			{
				drawer.IsSelected = true;

				if( drawer is HierarchyItemTransform )
				{
					Transform clickedTransform = ( (HierarchyItemTransform) drawer ).BoundTransform;
					CurrentSelection = clickedTransform;

					if( drawer is HierarchyItemSearchEntry && !clickedTransform.IsNull() )
					{
						// Fetch the object's path and show it in Hierarchy
						System.Text.StringBuilder sb = new System.Text.StringBuilder( 200 ).AppendLine( "Path:" );

						while( !clickedTransform.IsNull() )
						{
							sb.Append( "  " ).AppendLine( clickedTransform.name );
							clickedTransform = clickedTransform.parent;
						}

						selectedPathText.text = sb.Append( "  " ).Append( drawer.GetComponentInParent<HierarchyItemRoot>().Content.Name ).ToString();
						selectedPathBackground.gameObject.SetActive( true );
					}
				}
				else
					CurrentSelection = null;
			}
			else
				CurrentSelection = null;
		}

		public bool Select( Transform selection )
		{
			if( selection.IsNull() )
			{
				Deselect();
				return true;
			}
			else
			{
				if( selection == CurrentSelection )
					return true;

				Scene selectionScene = selection.gameObject.scene;
				for( int i = 0; i < sceneDrawers.Count; i++ )
				{
					IHierarchyRootContent content = sceneDrawers[i].Content;
					if( ( content is HierarchyRootPseudoScene ) || ( (HierarchyRootScene) content ).Scene == selectionScene )
					{
						HierarchyItem selectionItem = sceneDrawers[i].SelectTransform( selection );
						if( selectionItem != null )
						{
							if( drawAreaHierarchy.sizeDelta.y > 0f )
							{
								// Focus on selected HierarchyItem
								LayoutRebuilder.ForceRebuildLayoutImmediate( drawAreaHierarchy );
								Vector3 localPos = drawAreaHierarchy.InverseTransformPoint( selectionItem.transform.position );
								scrollView.verticalNormalizedPosition = Mathf.Clamp01( 1f + localPos.y / drawAreaHierarchy.sizeDelta.y );
							}

							return true;
						}
					}
				}
			}

			return false;
		}

		public void Deselect()
		{
			OnClicked( null );
		}

		private void OnSearchTermChanged( string search )
		{
			if( search != null )
				search = search.Trim();

			if( string.IsNullOrEmpty( search ) )
			{
				if( m_isInSearchMode )
				{
					scrollView.verticalNormalizedPosition = 1f;
					selectedPathBackground.gameObject.SetActive( false );
					m_isInSearchMode = false;
				}
			}
			else
			{
				if( !m_isInSearchMode )
				{
					scrollView.verticalNormalizedPosition = 1f;
					nextSearchRefreshTime = Time.realtimeSinceStartup + m_searchRefreshInterval;
					m_isInSearchMode = true;

					RefreshSearchResults();
					for( int i = 0; i < searchSceneDrawers.Count; i++ )
						searchSceneDrawers[i].IsExpanded = true;
				}
				else
					RefreshSearchResults();
			}

			drawAreaHierarchy.gameObject.SetActive( !m_isInSearchMode );
			drawAreaSearchResults.gameObject.SetActive( m_isInSearchMode );
			scrollView.content = m_isInSearchMode ? drawAreaSearchResults : drawAreaHierarchy;
		}

		private void OnSceneLoaded( Scene arg0, LoadSceneMode arg1 )
		{
			if( !ExposeUnityScenes )
				return;

			if( !arg0.IsValid() )
				return;

			for( int i = 0; i < sceneDrawers.Count; i++ )
			{
				if( ( sceneDrawers[i].Content is HierarchyRootScene ) && ( (HierarchyRootScene) sceneDrawers[i].Content ).Scene == arg0 )
					return;
			}

			HierarchyItemRoot sceneDrawer = InstantiateSceneDrawer( new HierarchyRootScene( arg0 ), drawAreaHierarchy );
			sceneDrawers.Add( sceneDrawer );

			HierarchyItemRoot searchResultDrawer = InstantiateSceneDrawer( new HierarchyRootSearch( this, sceneDrawer.Content ), drawAreaSearchResults );
			searchSceneDrawers.Add( searchResultDrawer );

			sceneDrawer.IsExpanded = true;
		}

		private void OnSceneUnloaded( Scene arg0 )
		{
			for( int i = 0; i < sceneDrawers.Count; i++ )
			{
				if( ( sceneDrawers[i].Content is HierarchyRootScene ) && ( (HierarchyRootScene) sceneDrawers[i].Content ).Scene == arg0 )
				{
					sceneDrawers[i].Unbind();
					sceneDrawers.RemoveAt( i );

					searchSceneDrawers[i].Unbind();
					searchSceneDrawers.RemoveAt( i );
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
			HierarchyRootPseudoScene pseudoScene = GetPseudoScene( scene, true );
			foreach( Transform transform in transforms )
				pseudoScene.AddChild( transform );
		}

		public void RemoveFromPseudoScene( string scene, Transform transform, bool deleteSceneIfEmpty )
		{
			HierarchyRootPseudoScene pseudoScene = GetPseudoScene( scene, false );
			if( pseudoScene == null )
				return;

			pseudoScene.RemoveChild( transform );

			if( deleteSceneIfEmpty && pseudoScene.Children.Count == 0 )
				DeletePseudoScene( scene );
		}

		public void RemoveFromPseudoScene( string scene, IEnumerable<Transform> transforms, bool deleteSceneIfEmpty )
		{
			HierarchyRootPseudoScene pseudoScene = GetPseudoScene( scene, false );
			if( pseudoScene == null )
				return;

			foreach( Transform transform in transforms )
				pseudoScene.RemoveChild( transform );

			if( deleteSceneIfEmpty && pseudoScene.Children.Count == 0 )
				DeletePseudoScene( scene );
		}

		private HierarchyRootPseudoScene GetPseudoScene( string scene, bool createIfNotExists )
		{
			HierarchyItemRoot drawer;
			if( pseudoSceneDrawers.TryGetValue( scene, out drawer ) )
				return (HierarchyRootPseudoScene) drawer.Content;

			if( createIfNotExists )
				return CreatePseudoSceneInternal( scene );

			return null;
		}

		public void CreatePseudoScene( string scene )
		{
			if( pseudoSceneDrawers.ContainsKey( scene ) )
				return;

			CreatePseudoSceneInternal( scene );
		}

		private HierarchyRootPseudoScene CreatePseudoSceneInternal( string scene )
		{
			int index = 0;
			for( int i = 0; i < pseudoScenesOrder.Length; i++ )
			{
				if( pseudoScenesOrder[i] == scene )
					break;

				if( pseudoSceneDrawers.ContainsKey( pseudoScenesOrder[i] ) )
					index++;
			}

			HierarchyItemRoot pseudoSceneDrawer = InstantiateSceneDrawer( new HierarchyRootPseudoScene( scene ), drawAreaHierarchy );
			sceneDrawers.Insert( index, pseudoSceneDrawer );
			pseudoSceneDrawers[scene] = pseudoSceneDrawer;

			HierarchyItemRoot searchResultDrawer = InstantiateSceneDrawer( new HierarchyRootSearch( this, pseudoSceneDrawer.Content ), drawAreaSearchResults );
			searchSceneDrawers.Insert( index, searchResultDrawer );

			pseudoSceneDrawer.transform.SetSiblingIndex( index );
			pseudoSceneDrawer.IsExpanded = true;

			searchResultDrawer.transform.SetSiblingIndex( index );

			return (HierarchyRootPseudoScene) pseudoSceneDrawer.Content;
		}

		public void DeleteAllPseudoScenes()
		{
			for( int i = sceneDrawers.Count - 1; i >= 0; i-- )
			{
				if( sceneDrawers[i].Content is HierarchyRootPseudoScene )
				{
					sceneDrawers[i].Unbind();
					sceneDrawers.RemoveAt( i );

					searchSceneDrawers[i].Unbind();
					searchSceneDrawers.RemoveAt( i );
				}
			}

			pseudoSceneDrawers.Clear();
		}

		public void DeletePseudoScene( string scene )
		{
			for( int i = 0; i < sceneDrawers.Count; i++ )
			{
				HierarchyRootPseudoScene pseudoScene = sceneDrawers[i].Content as HierarchyRootPseudoScene;
				if( pseudoScene != null && pseudoScene.Name == scene )
				{
					pseudoSceneDrawers.Remove( pseudoScene.Name );

					sceneDrawers[i].Unbind();
					sceneDrawers.RemoveAt( i );

					searchSceneDrawers[i].Unbind();
					searchSceneDrawers.RemoveAt( i );

					return;
				}
			}
		}

		public HierarchyItemRoot InstantiateSceneDrawer( IHierarchyRootContent target, Transform drawerParent )
		{
			HierarchyItemRoot sceneDrawer = (HierarchyItemRoot) InstantiateDrawer( sceneDrawerPool, sceneDrawerPrefab, drawerParent );
			sceneDrawer.BindTo( target );

			return sceneDrawer;
		}

		public HierarchyItemTransform InstantiateTransformDrawer( Transform drawerParent )
		{
			return (HierarchyItemTransform) InstantiateDrawer( transformDrawerPool, transformDrawerPrefab, drawerParent );
		}

		public HierarchyItemSearchEntry InstantiateSearchEntryDrawer( Transform drawerParent )
		{
			return (HierarchyItemSearchEntry) InstantiateDrawer( searchEntryDrawerPool, searchEntryDrawerPrefab, drawerParent );
		}

		private HierarchyItem InstantiateDrawer( List<HierarchyItem> drawerPool, HierarchyItem drawerPrefab, Transform drawerParent )
		{
			for( int i = drawerPool.Count - 1; i >= 0; i-- )
			{
				HierarchyItem instance = drawerPool[i];
				drawerPool.RemoveAt( i );

				if( !instance.IsNull() )
				{
					instance.transform.SetParent( drawerParent, false );
					instance.gameObject.SetActive( true );
					instance.Hierarchy = this;
					instance.Skin = Skin;

					return instance;
				}
			}

			HierarchyItem result = (HierarchyItem) Instantiate( drawerPrefab, drawerParent, false );
			result.Hierarchy = this;
			result.Skin = Skin;

			return result;
		}

		public void PoolDrawer( HierarchyItem drawer )
		{
			if( drawer == currentlySelectedDrawer )
			{
				currentlySelectedDrawer = null;
				m_currentSelection = null;
			}

			if( drawer is HierarchyItemTransform )
			{
				List<HierarchyItem> pool = drawer is HierarchyItemSearchEntry ? searchEntryDrawerPool : transformDrawerPool;
				if( pool.Count < poolCapacity )
				{
					drawer.gameObject.SetActive( false );
					drawer.transform.SetParent( poolParent, false );
					pool.Add( drawer );
				}
				else
					Destroy( drawer.gameObject );
			}
			else
			{
				drawer.gameObject.SetActive( false );
				drawer.transform.SetParent( poolParent, false );
				sceneDrawerPool.Add( drawer );
			}
		}
	}
}