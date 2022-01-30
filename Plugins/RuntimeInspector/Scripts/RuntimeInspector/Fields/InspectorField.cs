using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace RuntimeInspectorNamespace
{
	public abstract class InspectorField : MonoBehaviour, ITooltipContent
	{
		public delegate IReadOnlyList<T> Getter<out T>();
		public delegate void Setter<in T>( IReadOnlyList<T> value );

#pragma warning disable 0649
		[SerializeField]
		protected LayoutElement layoutElement;

		[SerializeField]
		protected Text variableNameText;

		[SerializeField]
		protected Image variableNameMask;

		[SerializeField]
		private MaskableGraphic visibleArea;
#pragma warning restore 0649

		private RuntimeInspector m_inspector;
		public RuntimeInspector Inspector
		{
			get { return m_inspector; }
			set
			{
				if( m_inspector != value )
				{
					m_inspector = value;
					OnInspectorChanged();
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
					OnDepthChanged();
				}
			}
		}

		protected Type m_boundVariableType;

		private int m_depth = -1;
		public int Depth
		{
			get { return m_depth; }
			set
			{
				if( m_depth != value )
				{
					m_depth = value;
					OnDepthChanged();
				}
			}
		}

		private bool m_isVisible = true;
		public bool IsVisible { get { return m_isVisible; } }

		public string Name
		{
			get { if( variableNameText ) return variableNameText.text; return string.Empty; }
			set { if( variableNameText ) variableNameText.text = Inspector.UseTitleCaseNaming ? value.ToTitleCase() : value; }
		}

		public string NameRaw
		{
			get { if( variableNameText ) return variableNameText.text; return string.Empty; }
			set { if( variableNameText ) variableNameText.text = value; }
		}

		bool ITooltipContent.IsActive { get { return this && gameObject.activeSelf; } }
		string ITooltipContent.TooltipText { get { return NameRaw; } }

		public virtual bool ShouldRefresh { get { return m_isVisible; } }

		protected virtual float HeightMultiplier { get { return 1f; } }

		public virtual void Initialize()
		{
			if( visibleArea )
				visibleArea.onCullStateChanged.AddListener( ( bool isCulled ) => m_isVisible = !isCulled );
		}

		public abstract bool SupportsType( Type type );

		public virtual bool CanBindTo( Type type, MemberInfo variable )
		{
			return true;
		}

		protected virtual void OnInspectorChanged()
		{
			if( !variableNameText )
				return;

			if( m_inspector.ShowTooltips )
			{
				TooltipArea tooltipArea = variableNameText.GetComponent<TooltipArea>();
				if( !tooltipArea )
					tooltipArea = variableNameText.gameObject.AddComponent<TooltipArea>();

				tooltipArea.Initialize( m_inspector.TooltipListener, this );
				variableNameText.raycastTarget = true;
			}
			else
			{
				TooltipArea tooltipArea = variableNameText.GetComponent<TooltipArea>();
				if( tooltipArea )
				{
					Destroy( tooltipArea );
					variableNameText.raycastTarget = false;
				}
			}
		}

		protected virtual void OnSkinChanged()
		{
			if( layoutElement )
				layoutElement.SetHeight( Skin.LineHeight * HeightMultiplier );

			if( variableNameText )
				variableNameText.SetSkinText( Skin );

			if( variableNameMask )
				variableNameMask.color = Skin.BackgroundColor;
		}

		protected virtual void OnDepthChanged()
		{
			if( variableNameText != null )
				variableNameText.rectTransform.sizeDelta = new Vector2( -Skin.IndentAmount * Depth, 0f );
		}

		/// Overload of BindTo that casts from <typeparam name="TParent"/> to the
		/// value type of the inspector field.
		public abstract void BindTo<TParent>(
			Type variableType,
			string variableName,
			Getter<TParent> getter,
			Setter<TParent> setter,
			MemberInfo variable = null);

		public abstract void BindTo<TParent>(
			InspectorField<TParent> parent,
			FieldInfo field,
			string variableName = null);

		public abstract void BindTo<TParent>(
			InspectorField<TParent> parent,
			PropertyInfo property,
			string variableName = null);

		public abstract void Refresh();
		public abstract void Unbind();
	}

	// Co-variant interface to reference a inspector field that binds only
	// Transform components as one that binds Components (IBound<Component>).
	// Think "The objects I bind to are components, but I don't necessarily bind
	// to all components".
	public interface IBound<out T>
	{
		IReadOnlyList<T> BoundValues { get; }
	}

	// Assume a inspector field can bind all objects. This contra-variant
	// interface allows you to reference it as a field that could potentially
	// bind Components (ISupportsType<Component>), and get all components from
	// it that it actually binds to.
	public interface ISupportsType<in T>
	{
		IEnumerable<D> GetBoundOfType<D>() where D : T;
	}

	public abstract class InspectorField<TBinding> : InspectorField, ISupportsType<TBinding>, IBound<TBinding>
	{
		private IReadOnlyList<TBinding> m_boundObjects = new TBinding[0];
		public IReadOnlyList<TBinding> BoundValues
		{
			get { return m_boundObjects; }
			protected set
			{
				setter( value );
				m_boundObjects = value;
			}
		}

		public IEnumerable<D> GetBoundOfType<D>() where D : TBinding
		{
			foreach( TBinding obj in m_boundObjects )
				if( typeof( D ).IsAssignableFrom( obj.GetType() ) )
					yield return (D) obj;
		}

		private Getter<TBinding> getter;
		private Setter<TBinding> setter;

		public override bool SupportsType( Type type )
		{
			return typeof( TBinding ).IsAssignableFrom( type );
		}

		// Level 2: Highest abstraction, for fields
		public override void BindTo<TParent>(
			InspectorField<TParent> parent,
			FieldInfo field,
			string variableName = null )
		{
			BindToImpl(
				parent,
				field,
				field.FieldType,
				instance => field.GetValue( instance ),
				( instance, value ) => field.SetValue( instance, value ),
				variableName );
		}

		// Level 2: Highest abstraction, for properties
		public override void BindTo<TParent>(
			InspectorField<TParent> parent,
			PropertyInfo property,
			string variableName = null)
		{
			BindToImpl(
				parent,
				property,
				property.PropertyType,
				instance => property.GetValue( instance ),
				( instance, value ) => property.SetValue( instance, value ),
				variableName );
		}

		// Level 2
		private void BindToImpl<TParent>(
			InspectorField<TParent> parent,
			MemberInfo member,
			Type memberType,
			Func<TParent, object> getter,
			Action<TParent, object> setter,
			string variableName )
		{
			if ( variableName == null )
				variableName = member.Name;

			BindTo(
				memberType,
				variableName,
				() => parent.BoundValues.Select( getter ),
				newValues =>
				{
					// Call different setter depending on whether TParent
					// is value- or reference type
	#if UNITY_EDITOR || !NETFX_CORE
					if( m_boundVariableType.IsValueType )
	#else
					if( m_boundVariableType.GetTypeInfo().IsValueType )
	#endif
					{
						parent.BoundValues = parent.BoundValues.Broadcast(
							newValues, ( x, y ) =>
							{
								setter( x, y );
								return x;
							} );
					}
					else
					{
						parent.BoundValues.Broadcast( newValues, setter );
					}
				},
				member);
		}

		// Level 1
		public override void BindTo<TParent>(
			Type variableType,
			string variableName,
			Getter<TParent> getter,
			Setter<TParent> setter,
			MemberInfo variable = null)
		{
			BindTo(
				variableType,
				variableName,
				() => getter().Cast<TParent, TBinding>(),
				o => setter( o.Cast<TBinding, TParent>() ),
				variable);
		}

		// Level 0: Most basic
		public void BindTo(
			Type variableType,
			string variableName,
			Getter<TBinding> getter,
			Setter<TBinding> setter,
			MemberInfo variable = null)
		{
			m_boundVariableType = variableType;
			Name = variableName;

			this.getter = getter;
			this.setter = setter;

			OnBound( variable );
		}

		public override void Unbind()
		{
			m_boundVariableType = null;

			getter = null;
			setter = null;

			OnUnbound();
			Inspector.PoolDrawer( this );
		}

		protected virtual void OnBound( MemberInfo variable )
		{
			RefreshValue();
		}

		protected virtual void OnUnbound()
		{
			m_boundObjects = new TBinding[0];
		}

		public override void Refresh()
		{
			RefreshValue();
		}

		private void RefreshValue()
		{
			try
			{
				m_boundObjects = getter();
			}
			catch
			{
#if UNITY_EDITOR || !NETFX_CORE
				if( m_boundVariableType.IsValueType )
#else
				if( m_boundVariableType.GetTypeInfo().IsValueType )
#endif
					m_boundObjects = new TBinding[1] { (TBinding) Activator.CreateInstance( m_boundVariableType ) };
				else
					m_boundObjects = new TBinding[0];
			}
		}
	}

	public interface IExpandableInspectorField
	{
		bool IsExpanded { get; set; }
		RuntimeInspector.HeaderVisibility HeaderVisibility { get; set; }
	}

	public abstract class ExpandableInspectorField<TBinding> : InspectorField<TBinding>, IExpandableInspectorField
	{
#pragma warning disable 0649
		[SerializeField]
		protected RectTransform drawArea;

		[SerializeField]
		private PointerEventListener expandToggle;
		private RectTransform expandToggleTransform;

		[SerializeField]
		private LayoutGroup layoutGroup;

		[SerializeField]
		private Image expandArrow; // Expand Arrow's sprite should look right at 0 rotation
#pragma warning restore 0649

		protected readonly List<InspectorField> elements = new List<InspectorField>( 8 );
		protected readonly List<ExposedMethodField> exposedMethods = new List<ExposedMethodField>();

		protected virtual int Length { get { return elements.Count; } }

		public override bool ShouldRefresh { get { return true; } }

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
			}
		}

		private RuntimeInspector.HeaderVisibility m_headerVisibility = RuntimeInspector.HeaderVisibility.Collapsible;
		public RuntimeInspector.HeaderVisibility HeaderVisibility
		{
			get { return m_headerVisibility; }
			set
			{
				if( m_headerVisibility != value )
				{
					if( m_headerVisibility == RuntimeInspector.HeaderVisibility.Hidden )
					{
						Depth++;
						layoutGroup.padding.top = Skin.LineHeight;
						expandToggle.gameObject.SetActive( true );
					}
					else if( value == RuntimeInspector.HeaderVisibility.Hidden )
					{
						Depth--;
						layoutGroup.padding.top = 0;
						expandToggle.gameObject.SetActive( false );
					}

					m_headerVisibility = value;

					if( m_headerVisibility == RuntimeInspector.HeaderVisibility.Collapsible )
					{
						if( expandArrow != null )
							expandArrow.gameObject.SetActive( true );

						variableNameText.rectTransform.sizeDelta = new Vector2( -( Skin.ExpandArrowSpacing + Skin.LineHeight * 0.5f ), 0f );
					}
					else if( m_headerVisibility == RuntimeInspector.HeaderVisibility.AlwaysVisible )
					{
						if( expandArrow != null )
							expandArrow.gameObject.SetActive( false );

						variableNameText.rectTransform.sizeDelta = new Vector2( 0f, 0f );

						if( !m_isExpanded )
							IsExpanded = true;
					}
					else
					{
						if( !m_isExpanded )
							IsExpanded = true;
					}
				}
			}
		}

		public override void Initialize()
		{
			base.Initialize();

			expandToggleTransform = (RectTransform) expandToggle.transform;
			expandToggle.PointerClick += ( eventData ) =>
			{
				if( m_headerVisibility == RuntimeInspector.HeaderVisibility.Collapsible )
					IsExpanded = !m_isExpanded;
			};

			IsExpanded = m_isExpanded;
		}

		protected override void OnUnbound()
		{
			base.OnUnbound();

			IsExpanded = false;
			HeaderVisibility = RuntimeInspector.HeaderVisibility.Collapsible;

			ClearElements();
		}

		protected override void OnInspectorChanged()
		{
			base.OnInspectorChanged();

			for( int i = 0; i < elements.Count; i++ )
				elements[i].Inspector = Inspector;
		}

		protected override void OnSkinChanged()
		{
			base.OnSkinChanged();

			Vector2 expandToggleSizeDelta = expandToggleTransform.sizeDelta;
			expandToggleSizeDelta.y = Skin.LineHeight;
			expandToggleTransform.sizeDelta = expandToggleSizeDelta;

			if( m_headerVisibility != RuntimeInspector.HeaderVisibility.Hidden )
			{
				layoutGroup.padding.top = Skin.LineHeight;

				if( m_headerVisibility == RuntimeInspector.HeaderVisibility.Collapsible )
					variableNameText.rectTransform.sizeDelta = new Vector2( -( Skin.ExpandArrowSpacing + Skin.LineHeight * 0.5f ), 0f );
			}

			if( expandArrow != null )
			{
				expandArrow.color = Skin.ExpandArrowColor;
				expandArrow.rectTransform.anchoredPosition = new Vector2( Skin.LineHeight * 0.25f, 0f );
				expandArrow.rectTransform.sizeDelta = new Vector2( Skin.LineHeight * 0.5f, Skin.LineHeight * 0.5f );
			}

			for( int i = 0; i < elements.Count; i++ )
				elements[i].Skin = Skin;

			for( int i = 0; i < exposedMethods.Count; i++ )
				exposedMethods[i].Skin = Skin;
		}

		protected override void OnDepthChanged()
		{
			Vector2 expandToggleSizeDelta = expandToggleTransform.sizeDelta;
			expandToggleSizeDelta.x = -Skin.IndentAmount * Depth;
			expandToggleTransform.sizeDelta = expandToggleSizeDelta;

			for( int i = 0; i < elements.Count; i++ )
				elements[i].Depth = Depth + 1;
		}

		protected void RegenerateElements()
		{
			if( elements.Count > 0 || exposedMethods.Count > 0 )
				ClearElements();

			if( Depth < Inspector.NestLimit )
			{
				drawArea.gameObject.SetActive( true );
				GenerateElements();
				GenerateExposedMethodButtons();
				drawArea.gameObject.SetActive( m_isExpanded );
			}
		}

		protected abstract void GenerateElements();

		private void GenerateExposedMethodButtons()
		{
			if( Inspector.ShowRemoveComponentButton
				&&  typeof( Component ).IsAssignableFrom( m_boundVariableType )
				&& !typeof( Transform ).IsAssignableFrom( m_boundVariableType ) )
			{
				CreateExposedMethodButton(
					GameObjectField.removeComponentMethod,
					() => new ExpandableInspectorField<TBinding>[] { this },
					value => { } );
			}

			ExposedMethod[] methods = m_boundVariableType.GetExposedMethods();
			if( methods != null )
			{
				bool isInitialized = BoundValues != null && !BoundValues.Equals( null );
				for( int i = 0; i < methods.Length; i++ )
				{
					ExposedMethod method = methods[i];
					if( ( isInitialized && method.VisibleWhenInitialized ) || ( !isInitialized && method.VisibleWhenUninitialized ) )
						CreateExposedMethodButton(
							method,
							() => BoundValues.Cast<TBinding, object>(),
							value => BoundValues = value.Cast<object, TBinding>() );
				}
			}
		}

		protected virtual void ClearElements()
		{
			for( int i = 0; i < elements.Count; i++ )
				elements[i].Unbind();

			for( int i = 0; i < exposedMethods.Count; i++ )
				exposedMethods[i].Unbind();

			elements.Clear();
			exposedMethods.Clear();
		}

		public override void Refresh()
		{
			base.Refresh();

			if( m_isExpanded )
			{
				if( Length != elements.Count )
					RegenerateElements();

				for( int i = 0; i < elements.Count; i++ )
				{
					if( elements[i].ShouldRefresh )
						elements[i].Refresh();
				}
			}
		}

		public InspectorField CreateDrawerForComponents( IReadOnlyList<Component> components, string variableName = null )
		{
			Type componentType = components[0].GetType();
			InspectorField variableDrawer = Inspector.CreateDrawerForType( componentType, drawArea, Depth + 1, false );
			if( variableDrawer != null )
			{
				if( variableName == null )
					variableName = componentType.Name + " component";

				variableDrawer.BindTo( componentType, string.Empty, () => components, ( value ) => { } );
				variableDrawer.NameRaw = variableName;

				elements.Add( variableDrawer );
			}

			return variableDrawer;
		}

		public InspectorField CreateDrawerForVariable( FieldInfo variable, string variableName = null )
		{
			InspectorField variableDrawer = Inspector.CreateDrawerForType( variable.FieldType, drawArea, Depth + 1, true, variable );
			if( variableDrawer != null )
			{
				variableDrawer.BindTo( this, variable, variableName == null ? null : string.Empty );
				if( variableName != null )
					variableDrawer.NameRaw = variableName;

				elements.Add( variableDrawer );
			}

			return variableDrawer;
		}

		public InspectorField CreateDrawerForVariable( PropertyInfo variable, string variableName = null )
		{
			InspectorField variableDrawer = Inspector.CreateDrawerForType( variable.PropertyType, drawArea, Depth + 1, true, variable );
			if( variableDrawer != null )
			{
				variableDrawer.BindTo( this, variable, variableName == null ? null : string.Empty );
				if( variableName != null )
					variableDrawer.NameRaw = variableName;

				elements.Add( variableDrawer );
			}

			return variableDrawer;
		}

		public InspectorField CreateDrawer<TChild>(
			string variableName,
			Func<TBinding, TChild> getter,
			Action<TBinding, TChild> setter,
			bool drawObjectsAsFields = true)
		{
			return CreateDrawer( typeof( TChild ), variableName, getter, setter, drawObjectsAsFields );
		}

		// Overload that handles multi-selection automatically. You don't pass
		// functions getting or setting a sequence of bound values, but instead
		// functions specifying how to convert a bound value of the parent drawer
		// to one of the child drawer.
		public InspectorField CreateDrawer<TChild>(
			Type variableType,
			string variableName,
			Func<TBinding, TChild> getter,
			Action<TBinding, TChild> setter,
			bool drawObjectsAsFields = true)
		{
			return CreateDrawer(
				variableType,
				variableName,
				() => BoundValues.Select( getter ),
				newChildObjs =>
				{
					foreach( TBinding instance in BoundValues )
						foreach( TChild value in newChildObjs )
							setter( instance, value );
				},
				drawObjectsAsFields);
		}

		protected InspectorField CreateDrawer<TChild>(
			string variableName,
			Getter<TChild> getter,
			Setter<TChild> setter,
			bool drawObjectsAsFields = true)
		{
			return CreateDrawer( typeof( TChild ), variableName, getter, setter, drawObjectsAsFields );
		}

		protected InspectorField CreateDrawer<TChild>(
			Type variableType,
			string variableName,
			Getter<TChild> getter,
			Setter<TChild> setter,
			bool drawObjectsAsFields = true)
		{
			InspectorField drawer = Inspector.CreateDrawerForType( variableType, drawArea, Depth + 1, drawObjectsAsFields );
			if( drawer == null )
				return null;

			if( variableName == null )
				variableName = string.Empty;
			else
				drawer.NameRaw = variableName;

			if( drawer is InspectorField<TChild> )
				( (InspectorField<TChild>) drawer ).BindTo( variableType, variableName, getter, setter );
			else
				// If there is no inspector field taking values of the correct type
				// directly, we use the overload that casts.
				drawer. BindTo( variableType, variableName, getter, setter );

			elements.Add( drawer );
			return drawer;
		}

		public ExposedMethodField CreateExposedMethodButton( ExposedMethod method, Getter<object> getter, Setter<object> setter )
		{
			ExposedMethodField methodDrawer = (ExposedMethodField) Inspector.CreateDrawerForType( typeof( ExposedMethod ), drawArea, Depth + 1, false );
			if( methodDrawer != null )
			{
				methodDrawer.BindTo( typeof( ExposedMethod ), string.Empty, getter, setter );
				methodDrawer.SetBoundMethod( method );

				exposedMethods.Add( methodDrawer );
			}

			return methodDrawer;
		}
	}
}
