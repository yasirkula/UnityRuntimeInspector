using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace RuntimeInspectorNamespace
{
	public abstract class InspectorField : MonoBehaviour, ITooltipContent
	{
		public delegate IEnumerable<T> Getter<out T>();
		public delegate void Setter<in T>( IEnumerable<T> value );

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

		public void BindTo<TParent>( IBound<TParent> parent, FieldInfo field, string variableName = null )
		{
			if( variableName == null )
				variableName = field.Name;
			BindTo(
				field.FieldType,
				variableName,
				() => parent.BoundValues.Cast<object>().Select( field.GetValue ),
				newValues => parent.BoundValues.Cast<object>().PassZipped( newValues, field.SetValue ),
				field);
		}

		public void BindTo<TParent>( IBound<TParent> parent, PropertyInfo property, string variableName = null )
		{
			if( variableName == null )
				variableName = property.Name;
			BindTo(
				property.PropertyType,
				variableName,
				() => parent.BoundValues.Cast<object>().Select( property.GetValue ),
				newValues => parent.BoundValues.Cast<object>().PassZipped( newValues, property.SetValue ),
				property);
		}

		public abstract void BindTo<TParent>(
			Type variableType,
			string variableName,
			Getter<TParent> getter,
			Setter<TParent> setter,
			MemberInfo variable = null);

		public abstract void Refresh();
		public abstract void Unbind();
	}

	public interface IBound<out T>
	{
		IEnumerable<T> BoundValues { get; }
	}

	public interface ISupportsType<in T>
	{
		IEnumerable<D> GetBoundOfType<D>() where D : T;
	}

	public abstract class InspectorField<T> : InspectorField, ISupportsType<T>, IBound<T>
	{
		private IEnumerable<T> m_boundObjects = new T[0];
		public IEnumerable<T> BoundValues
		{
			get { return m_boundObjects; }
			protected set
			{
				setter( value );
				m_boundObjects = value;
			}
		}

		public IEnumerable<D> GetBoundOfType<D>() where D : T
		{
			foreach( T obj in m_boundObjects )
				if( typeof( D ).IsAssignableFrom( obj.GetType() ) )
					yield return (D) obj;
		}

		private Getter<T> getter;
		private Setter<T> setter;

		public override bool SupportsType( Type type )
		{
			return typeof( T ).IsAssignableFrom( type );
		}

		public override void BindTo<P>(
			Type variableType,
			string variableName,
			Getter<P> getter,
			Setter<P> setter,
			MemberInfo variable = null)
		{
			BindTo(
				variableType,
				variableName,
				() => getter().Cast<T>(),
				o => setter( o.Cast<P>() ),
				variable);
		}

		public void BindTo(
			Type variableType,
			string variableName,
			Getter<T> getter,
			Setter<T> setter,
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
			m_boundObjects = new T[0];
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
					m_boundObjects = new T[1] { (T) Activator.CreateInstance( m_boundVariableType ) };
				else
					m_boundObjects = new T[0];
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
					() => new ExpandableInspectorField<T>[] { this },
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
							() => (IEnumerable<object>) BoundValues,
							value => BoundValues = value.Cast<T>() );
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

		public InspectorField CreateDrawerForComponents( IEnumerable<Component> components, string variableName = null )
		{
			Type componentType = components.First().GetType();
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

		public InspectorField<U> CreateDrawer<U>( string variableName, Func<T, U> getter, Action<T, U> setter, bool drawObjectsAsFields = true )
		{
				return CreateDrawer<U>(
					variableName,
					() => BoundValues.Select( getter ),
					newChildObjs =>
					{
						foreach( T o in BoundValues )
							foreach( U v in newChildObjs )
									setter( o, v );
					},
					drawObjectsAsFields);
		}

		public InspectorField<U> CreateDrawer<U>( string variableName, Getter<U> getter, Setter<U> setter, bool drawObjectsAsFields = true )
		{
			  InspectorField<U> variableDrawer = Inspector.CreateDrawerForType( typeof(U), drawArea, Depth + 1, drawObjectsAsFields ) as InspectorField<U>;
				if( variableDrawer != null )
				{
						variableDrawer.BindTo( typeof(U), variableName == null ? null : string.Empty, getter, setter );
						if( variableName != null )
							variableDrawer.NameRaw = variableName;

						elements.Add( variableDrawer );
				}

				return variableDrawer;
		}

		public InspectorField CreateDrawer( Type variableType, string variableName, Getter<object> getter, Setter<object> setter, bool drawObjectsAsFields = true )
		{
			InspectorField variableDrawer = Inspector.CreateDrawerForType( variableType, drawArea, Depth + 1, drawObjectsAsFields );
			if( variableDrawer != null )
			{
				variableDrawer.BindTo( variableType, variableName == null ? null : string.Empty, getter, setter );
				if( variableName != null )
					variableDrawer.NameRaw = variableName;

				elements.Add( variableDrawer );
			}

			return variableDrawer;
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
