using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace RuntimeInspectorNamespace
{
	public class RuntimeInspector : SkinnedWindow, ITooltipManager
	{
		public enum VariableVisibility { None = 0, SerializableOnly = 1, All = 2 };
		public enum HeaderVisibility { Collapsible = 0, AlwaysVisible = 1, Hidden = 2 };

		private const string POOL_OBJECT_NAME = "RuntimeInspectorPool";

		public delegate object InspectedObjectChangingDelegate( object previousInspectedObject, object newInspectedObject );
		public delegate void ComponentFilterDelegate( GameObject gameObject, List<Component> components );

#pragma warning disable 0649
		[SerializeField, UnityEngine.Serialization.FormerlySerializedAs( "refreshInterval" )]
		private float m_refreshInterval = 0f;
		private float nextRefreshTime = -1f;
		public float RefreshInterval
		{
			get { return m_refreshInterval; }
			set { m_refreshInterval = value; }
		}

		[Space]
		[SerializeField]
		private VariableVisibility m_exposeFields = VariableVisibility.SerializableOnly;
		public VariableVisibility ExposeFields
		{
			get { return m_exposeFields; }
			set
			{
				if( m_exposeFields != value )
				{
					m_exposeFields = value;
					isDirty = true;
				}
			}
		}

		[SerializeField]
		private VariableVisibility m_exposeProperties = VariableVisibility.SerializableOnly;
		public VariableVisibility ExposeProperties
		{
			get { return m_exposeProperties; }
			set
			{
				if( m_exposeProperties != value )
				{
					m_exposeProperties = value;
					isDirty = true;
				}
			}
		}

		[Space]
		[SerializeField]
		private bool m_arrayIndicesStartAtOne = false;
		public bool ArrayIndicesStartAtOne
		{
			get { return m_arrayIndicesStartAtOne; }
			set
			{
				if( m_arrayIndicesStartAtOne != value )
				{
					m_arrayIndicesStartAtOne = value;
					isDirty = true;
				}
			}
		}

		[SerializeField]
		private bool m_useTitleCaseNaming = false;
		public bool UseTitleCaseNaming
		{
			get { return m_useTitleCaseNaming; }
			set
			{
				if( m_useTitleCaseNaming != value )
				{
					m_useTitleCaseNaming = value;
					isDirty = true;
				}
			}
		}

		[Space]
		[SerializeField]
		private bool m_showAddComponentButton = true;
		public bool ShowAddComponentButton
		{
			get { return m_showAddComponentButton; }
			set
			{
				if( m_showAddComponentButton != value )
				{
					m_showAddComponentButton = value;
					isDirty = true;
				}
			}
		}

		[SerializeField]
		private bool m_showRemoveComponentButton = true;
		public bool ShowRemoveComponentButton
		{
			get { return m_showRemoveComponentButton; }
			set
			{
				if( m_showRemoveComponentButton != value )
				{
					m_showRemoveComponentButton = value;
					isDirty = true;
				}
			}
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
		private int m_nestLimit = 5;
		public int NestLimit
		{
			get { return m_nestLimit; }
			set
			{
				if( m_nestLimit != value )
				{
					m_nestLimit = value;
					isDirty = true;
				}
			}
		}

		[SerializeField]
		private HeaderVisibility m_inspectedObjectHeaderVisibility = HeaderVisibility.Collapsible;
		public HeaderVisibility InspectedObjectHeaderVisibility
		{
			get { return m_inspectedObjectHeaderVisibility; }
			set
			{
				if( m_inspectedObjectHeaderVisibility != value )
				{
					m_inspectedObjectHeaderVisibility = value;

					if( currentDrawer != null && currentDrawer is ExpandableInspectorField )
						( (ExpandableInspectorField) currentDrawer ).HeaderVisibility = m_inspectedObjectHeaderVisibility;
				}
			}
		}

		[SerializeField]
		private int poolCapacity = 10;
		private Transform poolParent;

		[SerializeField]
		private RuntimeHierarchy m_connectedHierarchy;
		public RuntimeHierarchy ConnectedHierarchy
		{
			get { return m_connectedHierarchy; }
			set { m_connectedHierarchy = value; }
		}

		[SerializeField]
		private RuntimeInspectorSettings[] settings;

		private bool m_isLocked = false;
		public bool IsLocked
		{
			get { return m_isLocked; }
			set { m_isLocked = value; }
		}

		[Header( "Internal Variables" )]
		[SerializeField]
		private ScrollRect scrollView;
		private RectTransform drawArea;

		[SerializeField]
		private Image background;

		[SerializeField]
		private Image scrollbar;
#pragma warning restore 0649

		private static int aliveInspectors = 0;

		private bool initialized = false;

		private readonly Dictionary<Type, InspectorField[]> typeToDrawers = new Dictionary<Type, InspectorField[]>( 89 );
		private readonly Dictionary<Type, InspectorField[]> typeToReferenceDrawers = new Dictionary<Type, InspectorField[]>( 89 );
		private readonly List<InspectorField> eligibleDrawers = new List<InspectorField>( 4 );

		private static readonly Dictionary<Type, List<InspectorField>> drawersPool = new Dictionary<Type, List<InspectorField>>();

		private readonly List<VariableSet> hiddenVariables = new List<VariableSet>( 32 );
		private readonly List<VariableSet> exposedVariables = new List<VariableSet>( 32 );

		private InspectorField currentDrawer = null;
		private bool inspectLock = false;
		private bool isDirty = false;

		private object m_inspectedObject;
		public object InspectedObject { get { return m_inspectedObject; } }

		public bool IsBound { get { return !m_inspectedObject.IsNull(); } }

		private Canvas m_canvas;
		public Canvas Canvas { get { return m_canvas; } }

		// Used to make sure that the scrolled content always remains within the scroll view's boundaries
		private PointerEventData nullPointerEventData;

		public InspectedObjectChangingDelegate OnInspectedObjectChanging;

		private ComponentFilterDelegate m_componentFilter;
		public ComponentFilterDelegate ComponentFilter
		{
			get { return m_componentFilter; }
			set
			{
				m_componentFilter = value;
				Refresh();
			}
		}

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

			drawArea = scrollView.content;
			m_canvas = GetComponentInParent<Canvas>();
			nullPointerEventData = new PointerEventData( null );

			if( m_showTooltips )
			{
				TooltipListener = gameObject.AddComponent<TooltipListener>();
				TooltipListener.Initialize( this );
			}

			GameObject poolParentGO = GameObject.Find( POOL_OBJECT_NAME );
			if( poolParentGO == null )
			{
				poolParentGO = new GameObject( POOL_OBJECT_NAME );
				DontDestroyOnLoad( poolParentGO );
			}

			poolParent = poolParentGO.transform;
			aliveInspectors++;

			for( int i = 0; i < settings.Length; i++ )
			{
				if( !settings[i] )
					continue;

				VariableSet[] hiddenVariablesForTypes = settings[i].HiddenVariables;
				for( int j = 0; j < hiddenVariablesForTypes.Length; j++ )
				{
					VariableSet hiddenVariablesSet = hiddenVariablesForTypes[j];
					if( hiddenVariablesSet.Init() )
						hiddenVariables.Add( hiddenVariablesSet );
				}

				VariableSet[] exposedVariablesForTypes = settings[i].ExposedVariables;
				for( int j = 0; j < exposedVariablesForTypes.Length; j++ )
				{
					VariableSet exposedVariablesSet = exposedVariablesForTypes[j];
					if( exposedVariablesSet.Init() )
						exposedVariables.Add( exposedVariablesSet );
				}
			}

			RuntimeInspectorUtils.IgnoredTransformsInHierarchy.Add( drawArea );
			RuntimeInspectorUtils.IgnoredTransformsInHierarchy.Add( poolParent );

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
			// On new Input System, scroll sensitivity is much higher than legacy Input system
			scrollView.scrollSensitivity *= 0.25f;
#endif
		}

		private void OnDestroy()
		{
			if( --aliveInspectors == 0 )
			{
				if( poolParent )
				{
					RuntimeInspectorUtils.IgnoredTransformsInHierarchy.Remove( poolParent );
					DestroyImmediate( poolParent.gameObject );
				}

				ColorPicker.DestroyInstance();
				ObjectReferencePicker.DestroyInstance();

				drawersPool.Clear();
			}

			RuntimeInspectorUtils.IgnoredTransformsInHierarchy.Remove( drawArea );
		}

		private void OnTransformParentChanged()
		{
			m_canvas = GetComponentInParent<Canvas>();
		}

#if UNITY_EDITOR
		protected override void OnValidate()
		{
			base.OnValidate();

			if( UnityEditor.EditorApplication.isPlaying )
				isDirty = true;
		}
#endif

		protected override void Update()
		{
			base.Update();

			if( IsBound )
			{
				float time = Time.realtimeSinceStartup;
				if( isDirty )
				{
					// Rebind to refresh the exposed variables in Inspector
					object inspectedObject = m_inspectedObject;
					StopInspectInternal();
					InspectInternal( inspectedObject );

					isDirty = false;
					nextRefreshTime = time + m_refreshInterval;
				}
				else
				{
					if( time > nextRefreshTime )
					{
						nextRefreshTime = time + m_refreshInterval;
						Refresh();
					}
				}
			}
			else if( currentDrawer != null )
				StopInspectInternal();
		}

		public void Refresh()
		{
			if( IsBound )
			{
				if( currentDrawer == null )
					m_inspectedObject = null;
				else
					currentDrawer.Refresh();
			}
		}

		// Refreshes the Inspector in the next Update. Called by most of the InspectorDrawers
		// when their values have changed. If a drawer is bound to a property whose setter
		// may modify the input data (e.g. when input data is 20 but the setter clamps it to 10),
		// the drawer's BoundInputFields will still show the unmodified input data (20) until the
		// next Refresh. That is because BoundInputFields don't have access to the fields/properties 
		// they are modifying, there is no way for the BoundInputFields to know whether or not
		// the property has modified the input data (changed it from 20 to 10).
		// 
		// Why not refresh only the changed InspectorDrawers? Because changing a property may affect
		// multiple fields/properties that are bound to other drawers, we don't know which
		// drawers may be affected. The safest way is to refresh all the drawers.
		// 
		// Why not Refresh? That's the hacky part: most drawers call this function in their
		// BoundInputFields' OnValueSubmitted event. If Refresh was used, BoundInputField's
		// "recentText = str;" line that is called after the OnValueSubmitted event would mess up
		// with refreshing the value displayed on the BoundInputField.
		public void RefreshDelayed()
		{
			nextRefreshTime = 0f;
		}

		// Makes sure that scroll view's contents are within scroll view's bounds
		internal void EnsureScrollViewIsWithinBounds()
		{
			// When scrollbar is snapped to the very bottom of the scroll view, sometimes OnScroll alone doesn't work
			if( scrollView.verticalNormalizedPosition <= Mathf.Epsilon )
				scrollView.verticalNormalizedPosition = 0.0001f;

			scrollView.OnScroll( nullPointerEventData );
		}

		protected override void RefreshSkin()
		{
			background.color = Skin.BackgroundColor;
			scrollbar.color = Skin.ScrollbarColor;

			if( IsBound && !isDirty )
				currentDrawer.Skin = Skin;
		}

		public void Inspect( object obj )
		{
			if( !m_isLocked )
				InspectInternal( obj );
		}

		internal void InspectInternal( object obj )
		{
			if( inspectLock )
				return;

			isDirty = false;
			Initialize();

			if( OnInspectedObjectChanging != null )
				obj = OnInspectedObjectChanging( m_inspectedObject, obj );

			if( m_inspectedObject == obj )
				return;

			StopInspectInternal();

			inspectLock = true;
			try
			{
				m_inspectedObject = obj;

				if( obj.IsNull() )
					return;

#if UNITY_EDITOR || !NETFX_CORE
				if( obj.GetType().IsValueType )
#else
				if( obj.GetType().GetTypeInfo().IsValueType )
#endif
				{
					m_inspectedObject = null;
					Debug.LogError( "Can't inspect a value type!" );
					return;
				}

				//if( !gameObject.activeSelf )
				//{
				//	m_inspectedObject = null;
				//	Debug.LogError( "Can't inspect while Inspector is inactive!" );
				//	return;
				//}

				InspectorField inspectedObjectDrawer = CreateDrawerForType( obj.GetType(), drawArea, 0, false );
				if( inspectedObjectDrawer != null )
				{
					inspectedObjectDrawer.BindTo( obj.GetType(), string.Empty, () => m_inspectedObject, ( value ) => m_inspectedObject = value );
					inspectedObjectDrawer.NameRaw = obj.GetNameWithType();
					inspectedObjectDrawer.Refresh();

					if( inspectedObjectDrawer is ExpandableInspectorField )
						( (ExpandableInspectorField) inspectedObjectDrawer ).IsExpanded = true;

					currentDrawer = inspectedObjectDrawer;
					if( currentDrawer is ExpandableInspectorField )
						( (ExpandableInspectorField) currentDrawer ).HeaderVisibility = m_inspectedObjectHeaderVisibility;

					GameObject go = m_inspectedObject as GameObject;
					if( !go && m_inspectedObject as Component )
						go = ( (Component) m_inspectedObject ).gameObject;

					if( ConnectedHierarchy && go && !ConnectedHierarchy.Select( go.transform, RuntimeHierarchy.SelectOptions.FocusOnSelection ) )
						ConnectedHierarchy.Deselect();
				}
				else
					m_inspectedObject = null;
			}
			finally
			{
				inspectLock = false;
			}
		}

		public void StopInspect()
		{
			if( !m_isLocked )
				StopInspectInternal();
		}

		internal void StopInspectInternal()
		{
			if( inspectLock )
				return;

			if( currentDrawer != null )
			{
				currentDrawer.Unbind();
				currentDrawer = null;
			}

			m_inspectedObject = null;
			scrollView.verticalNormalizedPosition = 1f;

			ColorPicker.Instance.Close();
			ObjectReferencePicker.Instance.Close();
		}

		public InspectorField CreateDrawerForType( Type type, Transform drawerParent, int depth, bool drawObjectsAsFields = true, MemberInfo variable = null )
		{
			InspectorField[] variableDrawers = GetDrawersForType( type, drawObjectsAsFields );
			if( variableDrawers != null )
			{
				for( int i = 0; i < variableDrawers.Length; i++ )
				{
					if( variableDrawers[i].CanBindTo( type, variable ) )
					{
						InspectorField drawer = InstantiateDrawer( variableDrawers[i], drawerParent );
						drawer.Inspector = this;
						drawer.Skin = Skin;
						drawer.Depth = depth;

						return drawer;
					}
				}
			}

			return null;
		}

		private InspectorField InstantiateDrawer( InspectorField drawer, Transform drawerParent )
		{
			List<InspectorField> drawerPool;
			if( drawersPool.TryGetValue( drawer.GetType(), out drawerPool ) )
			{
				for( int i = drawerPool.Count - 1; i >= 0; i-- )
				{
					InspectorField instance = drawerPool[i];
					drawerPool.RemoveAt( i );

					if( instance )
					{
						instance.transform.SetParent( drawerParent, false );
						instance.gameObject.SetActive( true );

						return instance;
					}
				}
			}

			InspectorField newDrawer = (InspectorField) Instantiate( drawer, drawerParent, false );
			newDrawer.Initialize();
			return newDrawer;
		}

		private InspectorField[] GetDrawersForType( Type type, bool drawObjectsAsFields )
		{
			bool searchReferenceFields = drawObjectsAsFields && typeof( Object ).IsAssignableFrom( type );

			InspectorField[] cachedResult;
			if( ( searchReferenceFields && typeToReferenceDrawers.TryGetValue( type, out cachedResult ) ) ||
				( !searchReferenceFields && typeToDrawers.TryGetValue( type, out cachedResult ) ) )
				return cachedResult;

			Dictionary<Type, InspectorField[]> drawersDict = searchReferenceFields ? typeToReferenceDrawers : typeToDrawers;

			eligibleDrawers.Clear();
			for( int i = settings.Length - 1; i >= 0; i-- )
			{
				InspectorField[] drawers = searchReferenceFields ? settings[i].ReferenceDrawers : settings[i].StandardDrawers;
				for( int j = drawers.Length - 1; j >= 0; j-- )
				{
					if( drawers[j].SupportsType( type ) )
						eligibleDrawers.Add( drawers[j] );
				}
			}

			cachedResult = eligibleDrawers.Count > 0 ? eligibleDrawers.ToArray() : null;
			drawersDict[type] = cachedResult;

			return cachedResult;
		}

		internal void PoolDrawer( InspectorField drawer )
		{
			List<InspectorField> drawerPool;
			if( !drawersPool.TryGetValue( drawer.GetType(), out drawerPool ) )
			{
				drawerPool = new List<InspectorField>( poolCapacity );
				drawersPool[drawer.GetType()] = drawerPool;
			}

			if( drawerPool.Count < poolCapacity )
			{
				drawer.gameObject.SetActive( false );
				drawer.transform.SetParent( poolParent, false );
				drawerPool.Add( drawer );
			}
			else
				Destroy( drawer.gameObject );
		}

		internal ExposedVariablesEnumerator GetExposedVariablesForType( Type type )
		{
			MemberInfo[] allVariables = type.GetAllVariables();
			if( allVariables == null )
				return new ExposedVariablesEnumerator( null, null, null, VariableVisibility.None, VariableVisibility.None );

			List<VariableSet> hiddenVariablesForType = null;
			List<VariableSet> exposedVariablesForType = null;
			for( int i = 0; i < hiddenVariables.Count; i++ )
			{
				if( hiddenVariables[i].type.IsAssignableFrom( type ) )
				{
					if( hiddenVariablesForType == null )
						hiddenVariablesForType = new List<VariableSet>() { hiddenVariables[i] };
					else
						hiddenVariablesForType.Add( hiddenVariables[i] );
				}
			}

			for( int i = 0; i < exposedVariables.Count; i++ )
			{
				if( exposedVariables[i].type.IsAssignableFrom( type ) )
				{
					if( exposedVariablesForType == null )
						exposedVariablesForType = new List<VariableSet>() { exposedVariables[i] };
					else
						exposedVariablesForType.Add( exposedVariables[i] );
				}
			}

			return new ExposedVariablesEnumerator( allVariables, hiddenVariablesForType, exposedVariablesForType, m_exposeFields, m_exposeProperties );
		}
	}
}