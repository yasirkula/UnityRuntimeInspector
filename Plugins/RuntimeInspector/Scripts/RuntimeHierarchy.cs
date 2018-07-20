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

		[SerializeField]
		private float refreshInterval = 0f;
		private float nextRefreshTime = -1f;

		[SerializeField]
		private float m_objectNamesRefreshInterval = 10f;
		public float ObjectNamesRefreshInterval { get { return m_objectNamesRefreshInterval; } }

		[SerializeField]
		private int poolCapacity = 64;
		private Transform poolParent;
		private static int aliveHierarchies = 0;
		private static List<HierarchyItem> sceneDrawerPool = new List<HierarchyItem>( 8 );
		private static List<HierarchyItem> transformDrawerPool;

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
		private float doubleClickThreshold = 0.5f;
		private float lastClickTime;

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

		[SerializeField]
		private ScrollRect scrollView;
		private RectTransform drawArea;

		[SerializeField]
		private Image background;

		[SerializeField]
		private Image scrollbar;

		private List<HierarchyItemRoot> sceneDrawers = new List<HierarchyItemRoot>( 8 );

		[SerializeField]
		private HierarchyItemRoot sceneDrawerPrefab;

		[SerializeField]
		private HierarchyItemTransform transformDrawerPrefab;

		private HierarchyItem currentlySelectedDrawer = null;

		private Dictionary<string, HierarchyItemRoot> pseudoSceneDrawers = new Dictionary<string, HierarchyItemRoot>();

		public event SelectionChangedDelegate OnSelectionChanged;
		public event DoubleClickDelegate OnItemDoubleClicked;

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

		protected override void Awake()
		{
			base.Awake();

			drawArea = scrollView.content;

			if( transformDrawerPool == null )
				transformDrawerPool = new List<HierarchyItem>( poolCapacity );

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
					DestroyImmediate( poolParent.gameObject );

				sceneDrawerPool.Clear();

				if( transformDrawerPool != null )
					transformDrawerPool.Clear();
			}
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

			if( Time.realtimeSinceStartup > nextRefreshTime )
			{
				nextRefreshTime = Time.realtimeSinceStartup + refreshInterval;
				Refresh();
			}
		}

		public void Refresh()
		{
			for( int i = 0; i < sceneDrawers.Count; i++ )
				sceneDrawers[i].Refresh();
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
			}
		}

		protected override void RefreshSkin()
		{
			background.color = Skin.BackgroundColor;
			scrollbar.color = Skin.ScrollbarColor;

			for( int i = 0; i < sceneDrawers.Count; i++ )
				sceneDrawers[i].Skin = Skin;

			LayoutRebuilder.ForceRebuildLayoutImmediate( drawArea );
		}

		public void OnClicked( HierarchyItem drawer )
		{
			if( currentlySelectedDrawer == drawer )
			{
				if( OnItemDoubleClicked != null )
				{
					if( !drawer.IsNull() && Time.realtimeSinceStartup - lastClickTime <= doubleClickThreshold )
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
					CurrentSelection = ( (HierarchyItemTransform) drawer ).BoundTransform;
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
							if( drawArea.sizeDelta.y > 0f )
							{
								// Focus on selected HierarchyItem
								LayoutRebuilder.ForceRebuildLayoutImmediate( drawArea );
								Vector3 localPos = drawArea.InverseTransformPoint( selectionItem.transform.position );
								scrollView.verticalNormalizedPosition = Mathf.Clamp01( 1f + localPos.y / drawArea.sizeDelta.y );
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

			HierarchyItemRoot sceneDrawer = InstantiateSceneDrawer( new HierarchyRootScene( arg0 ) );
			sceneDrawers.Add( sceneDrawer );

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

			if( deleteSceneIfEmpty && pseudoScene.ChildCount == 0 )
				DeletePseudoScene( scene );
		}

		public void RemoveFromPseudoScene( string scene, IEnumerable<Transform> transforms, bool deleteSceneIfEmpty )
		{
			HierarchyRootPseudoScene pseudoScene = GetPseudoScene( scene, false );
			if( pseudoScene == null )
				return;

			foreach( Transform transform in transforms )
				pseudoScene.RemoveChild( transform );

			if( deleteSceneIfEmpty && pseudoScene.ChildCount == 0 )
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
			HierarchyItemRoot pseudoSceneDrawer = InstantiateSceneDrawer( new HierarchyRootPseudoScene( scene ) );
			sceneDrawers.Add( pseudoSceneDrawer );
			pseudoSceneDrawers[scene] = pseudoSceneDrawer;

			int index = 0;
			for( int i = 0; i < pseudoScenesOrder.Length; i++ )
			{
				if( pseudoScenesOrder[i] == scene )
					break;

				if( pseudoSceneDrawers.ContainsKey( pseudoScenesOrder[i] ) )
					index++;
			}

			pseudoSceneDrawer.transform.SetSiblingIndex( index );

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

					return;
				}
			}
		}

		private HierarchyItemRoot InstantiateSceneDrawer( IHierarchyRootContent target )
		{
			HierarchyItemRoot sceneDrawer = (HierarchyItemRoot) InstantiateDrawer( sceneDrawerPool, sceneDrawerPrefab, drawArea );
			sceneDrawer.BindTo( target );

			return sceneDrawer;
		}

		public HierarchyItemTransform InstantiateTransformDrawer( Transform drawerParent )
		{
			return (HierarchyItemTransform) InstantiateDrawer( transformDrawerPool, transformDrawerPrefab, drawerParent );
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
				HierarchyItemTransform transformDrawer = (HierarchyItemTransform) drawer;
				if( transformDrawerPool.Count < poolCapacity )
				{
					drawer.gameObject.SetActive( false );
					drawer.transform.SetParent( poolParent, false );
					transformDrawerPool.Add( transformDrawer );
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