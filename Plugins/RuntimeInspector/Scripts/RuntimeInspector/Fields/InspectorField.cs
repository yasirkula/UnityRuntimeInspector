using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace RuntimeInspectorNamespace
{
	public abstract class IInspectorField : MonoBehaviour, ITooltipContent
	{
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

		public abstract void Refresh();
		public abstract void Unbind();
	}

	public abstract class InspectorField<T> : IInspectorField
	{
		private T m_value;
		public T Value
		{
			get { return m_value; }
			protected set
			{
				// try { setter( value ); m_value = value; }
				// catch { }
				setter( value ); m_value = value;
			}
		}

		private Func<T> getter;
		private Action<T> setter;

		public override bool SupportsType( Type type )
		{
			return typeof( T ).IsAssignableFrom( type );
		}

		public void BindTo<P>( InspectorField<P> parent, FieldInfo field, string variableName = null )
		{
			if( variableName == null )
				variableName = field.Name;

#if UNITY_EDITOR || !NETFX_CORE
			if( !parent.m_boundVariableType.IsValueType )
#else
			if( !parent.BoundVariableType.GetTypeInfo().IsValueType )
#endif
				BindTo( field.FieldType, variableName, () => (T) field.GetValue(parent.Value), ( value ) => field.SetValue( parent.Value, value ), field );
			else
				BindTo( field.FieldType, variableName, () => (T) field.GetValue( parent.Value ), ( value ) =>
				{
					field.SetValue( parent.Value, value );
					parent.Value = parent.Value;
				}, field );
		}

		public void BindTo<P>( InspectorField<P> parent, PropertyInfo property, string variableName = null )
		{
			if( variableName == null )
				variableName = property.Name;

#if UNITY_EDITOR || !NETFX_CORE
			if( !parent.m_boundVariableType.IsValueType )
#else
			if( !parent.BoundVariableType.GetTypeInfo().IsValueType )
#endif
				BindTo( property.PropertyType, variableName, () => (T) property.GetValue( parent.Value, null ), ( value ) => property.SetValue( parent.Value, value, null ), property );
			else
				BindTo( property.PropertyType, variableName, () => (T) property.GetValue( parent.Value, null ), ( value ) =>
				{
					property.SetValue( parent.Value, value, null );
					parent.Value = parent.Value;
				}, property );
		}

		public void BindTo( Type variableType, string variableName, Func<T> getter, Action<T> setter, MemberInfo variable = null )
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
			m_value = default(T);
		}


		public override void Refresh()
		{
			RefreshValue();
		}

		private void RefreshValue()
		{
			try
			{
				m_value = getter();
			}
			catch
			{
#if UNITY_EDITOR || !NETFX_CORE
				if( m_boundVariableType.IsValueType )
#else
				if( m_boundVariableType.GetTypeInfo().IsValueType )
#endif
					m_value = (T) Activator.CreateInstance( m_boundVariableType );
				else
					m_value = default(T);
			}
		}
	}

	public interface IExpandableInspectorField
	{
		public bool IsExpanded { get; set; }
		public RuntimeInspector.HeaderVisibility HeaderVisibility { get; set; }
	}

	public abstract class ExpandableInspectorField<T> : InspectorField<T>, IExpandableInspectorField
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

		protected readonly List<IInspectorField> elements = new List<IInspectorField>( 8 );
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
			if( Inspector.ShowRemoveComponentButton && typeof( Component ).IsAssignableFrom( m_boundVariableType ) && !typeof( Transform ).IsAssignableFrom( m_boundVariableType ) )
				CreateExposedMethodButton( GameObjectField.removeComponentMethod, () => this, ( value ) => { } );

			ExposedMethod[] methods = m_boundVariableType.GetExposedMethods();
			if( methods != null )
			{
				bool isInitialized = Value != null && !Value.Equals( null );
				for( int i = 0; i < methods.Length; i++ )
				{
					ExposedMethod method = methods[i];
					if( ( isInitialized && method.VisibleWhenInitialized ) || ( !isInitialized && method.VisibleWhenUninitialized ) )
						CreateExposedMethodButton( method, () => Value, ( value ) => Value = (T) value );
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

		public InspectorField<Component> CreateDrawerForComponent( Component component, string variableName = null )
		{
			InspectorField<Component> variableDrawer = Inspector.CreateDrawerForType<Component>( component.GetType(), drawArea, Depth + 1, false );
			if( variableDrawer != null )
			{
				if( variableName == null )
					variableName = component.GetType().Name + " component";

				variableDrawer.BindTo( component.GetType(), string.Empty, () => component, ( value ) => { } );
				variableDrawer.NameRaw = variableName;

				elements.Add( variableDrawer );
			}

			return variableDrawer;
		}

		public InspectorField<U> CreateDrawerForVariable<U>( MemberInfo variable, string variableName = null )
		{
				if( variable is FieldInfo field )
						return CreateDrawerForVariable<U>( field, variableName );
				if( variable is PropertyInfo property )
						return CreateDrawerForVariable<U>( property, variableName );
				throw new ArgumentException( "Variable can either be a field or a property" );
		}

		public InspectorField<U> CreateDrawerForVariable<U>( FieldInfo variable, string variableName = null )
		{
			InspectorField<U> variableDrawer = Inspector.CreateDrawerForType<U>( variable.FieldType, drawArea, Depth + 1, true, variable );
			if( variableDrawer != null )
			{
				variableDrawer.BindTo( this, variable, variableName == null ? null : string.Empty );
				if( variableName != null )
					variableDrawer.NameRaw = variableName;

				elements.Add( variableDrawer );
			}

			return variableDrawer;
		}

		public InspectorField<U> CreateDrawerForVariable<U>( PropertyInfo variable, string variableName = null )
		{
			InspectorField<U> variableDrawer = Inspector.CreateDrawerForType<U>( variable.PropertyType, drawArea, Depth + 1, true, variable );
			if( variableDrawer != null )
			{
				variableDrawer.BindTo( this, variable, variableName == null ? null : string.Empty );
				if( variableName != null )
					variableDrawer.NameRaw = variableName;

				elements.Add( variableDrawer );
			}

			return variableDrawer;
		}

		public InspectorField<U> CreateDrawer<U>( Type variableType, string variableName, Func<U> getter, Action<U> setter, bool drawObjectsAsFields = true )
		{
			InspectorField<U> variableDrawer = Inspector.CreateDrawerForType<U>( variableType, drawArea, Depth + 1, drawObjectsAsFields );
			if( variableDrawer != null )
			{
				variableDrawer.BindTo( variableType, variableName == null ? null : string.Empty, getter, setter );
				if( variableName != null )
					variableDrawer.NameRaw = variableName;

				elements.Add( variableDrawer );
			}

			return variableDrawer;
		}

		public ExposedMethodField CreateExposedMethodButton( ExposedMethod method, Func<object> getter, Action<object> setter )
		{
			ExposedMethodField methodDrawer = (ExposedMethodField) Inspector.CreateDrawerForType<object>( typeof( ExposedMethod ), drawArea, Depth + 1, false );
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
