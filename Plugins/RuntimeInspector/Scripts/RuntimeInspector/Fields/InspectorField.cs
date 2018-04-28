using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace RuntimeInspectorNamespace
{
	public abstract class InspectorField : MonoBehaviour
	{
		public delegate object Getter();
		public delegate void Setter( object value );
		
		private RuntimeInspector m_inspector;
		public RuntimeInspector Inspector
		{
			protected get { return m_inspector; }
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
			protected get { return m_skin; }
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

		private Type m_boundVariableType;
        protected Type BoundVariableType { get { return m_boundVariableType; } }

		private object m_value;
		protected object Value
		{
			get { return m_value; }
			set
			{
				try { setter( value ); m_value = value; }
				catch { }
			}
		}

		private int m_depth = -1;
		public int Depth
		{
			protected get { return m_depth; }
			set
			{
				if( m_depth != value )
				{
					m_depth = value;
					OnDepthChanged();
				}
			}
		}

		public string Name
		{
			get { if( variableNameText != null ) return variableNameText.text; return string.Empty; }
			set { if( variableNameText != null ) variableNameText.text = Inspector.UseTitleCaseNaming ? value.ToTitleCase() : value; }
		}

		public string NameRaw
		{
			get { if( variableNameText != null ) return variableNameText.text; return string.Empty; }
			set { if( variableNameText != null ) variableNameText.text = value; }
		}

		protected virtual float HeightMultiplier { get { return 1f; } }

		[SerializeField]
		protected LayoutElement layoutElement;

		[SerializeField]
		protected Text variableNameText;

		[SerializeField]
		private Image variableNameMask;

		private Getter getter;
		private Setter setter;
		
		public abstract bool SupportsType( Type type );

		public virtual void Initialize() { }

		public void BindTo( InspectorField parent, MemberInfo member, string variableName = null )
		{
			if( member is FieldInfo )
			{
				FieldInfo field = (FieldInfo) member;
				if( variableName == null )
					variableName = field.Name;

#if UNITY_EDITOR || !NETFX_CORE
				if( !parent.BoundVariableType.IsValueType )
#else
				if( !parent.BoundVariableType.GetTypeInfo().IsValueType )
#endif
					BindTo( field.FieldType, variableName, () => field.GetValue( parent.Value ), ( value ) => field.SetValue( parent.Value, value ) );
				else
					BindTo( field.FieldType, variableName, () => field.GetValue( parent.Value ), ( value ) =>
					{
						field.SetValue( parent.Value, value );
						parent.Value = parent.Value;
					} );
			}
			else if( member is PropertyInfo )
			{
				PropertyInfo property = (PropertyInfo) member;
				if( variableName == null )
					variableName = property.Name;

#if UNITY_EDITOR || !NETFX_CORE
				if( !parent.BoundVariableType.IsValueType )
#else
				if( !parent.BoundVariableType.GetTypeInfo().IsValueType )
#endif
					BindTo( property.PropertyType, variableName, () => property.GetValue( parent.Value, null ), ( value ) => property.SetValue( parent.Value, value, null ) );
				else
					BindTo( property.PropertyType, variableName, () => property.GetValue( parent.Value, null ), ( value ) =>
					{
						property.SetValue( parent.Value, value, null );
						parent.Value = parent.Value;
					} );
			}
			else
				throw new ArgumentException( "Member can either be a field or a property" );
		}

		public void BindTo( Type variableType, string variableName, Getter getter, Setter setter )
		{
			m_boundVariableType = variableType;
			Name = variableName;

			this.getter = getter;
			this.setter = setter;

			OnBound();
		}

		public void Unbind()
		{
			m_boundVariableType = null;

			getter = null;
			setter = null;

			OnUnbound();
			Inspector.PoolDrawer( this );
        }
		
		protected virtual void OnBound()
		{
			RefreshValue();
		}

		protected virtual void OnUnbound()
		{
			m_value = null;
        }

		protected virtual void OnInspectorChanged() { }

		protected virtual void OnSkinChanged()
		{
			if( layoutElement != null )
				layoutElement.SetHeight( Skin.LineHeight * HeightMultiplier );

			if( variableNameText != null )
				variableNameText.SetSkinText( Skin );

			if( variableNameMask != null )
				variableNameMask.color = Skin.BackgroundColor;
		}

		protected virtual void OnDepthChanged()
		{
			if( variableNameText != null )
				variableNameText.rectTransform.sizeDelta = new Vector2( -Skin.IndentAmount * Depth, 0f );
		}

		public virtual void Refresh()
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
				if( BoundVariableType.IsValueType )
#else
				if( BoundVariableType.GetTypeInfo().IsValueType )
#endif
					m_value = Activator.CreateInstance( BoundVariableType );
				else
					m_value = null;
			}
		}
	}

	public abstract class ExpandableInspectorField : InspectorField
	{
		[SerializeField]
		protected RectTransform drawArea;

		[SerializeField]
		private PointerEventListener expandToggle;
		private RectTransform expandToggleTransform;

		[SerializeField]
		private LayoutGroup layoutGroup;

		[SerializeField]
		private Image expandArrow; // Expand Arrow's sprite should look right at 0 rotation
		
		protected List<InspectorField> elements = new List<InspectorField>( 8 );
		private List<ExposedMethodField> exposedMethods = new List<ExposedMethodField>();

		protected virtual int Length { get { return elements.Count; } }

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

		public override void Initialize()
		{
			base.Initialize();
			
			expandToggleTransform = (RectTransform) expandToggle.transform;
			expandToggle.PointerClick += (eventData) => IsExpanded = !m_isExpanded;
			IsExpanded = m_isExpanded;
		}

		protected override void OnUnbound()
		{
			base.OnUnbound();

			IsExpanded = false;
			ClearElements();
		}

		protected override void OnSkinChanged()
		{
			base.OnSkinChanged();

			Vector2 expandToggleSizeDelta = expandToggleTransform.sizeDelta;
			expandToggleSizeDelta.y = Skin.LineHeight;
			expandToggleTransform.sizeDelta = expandToggleSizeDelta;
			
			layoutGroup.padding.top = Skin.LineHeight;
			expandArrow.color = Skin.ExpandArrowColor;

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
				GenerateMethods();
				drawArea.gameObject.SetActive( m_isExpanded );
			}
		}

		protected abstract void GenerateElements();

		private void GenerateMethods()
		{
			ExposedMethod[] methods = BoundVariableType.GetExposedMethods();
			if( methods != null )
			{
				bool isInitialized = Value != null && !Value.Equals( null );
				for( int i = 0; i < methods.Length; i++ )
				{
					ExposedMethod method = methods[i];
					if( ( isInitialized && method.VisibleWhenInitialized ) || ( !isInitialized && method.VisibleWhenUninitialized ) )
					{
						ExposedMethodField methodDrawer = (ExposedMethodField) Inspector.CreateDrawerForType( typeof( ExposedMethod ), drawArea, Depth + 1, false );
						if( methodDrawer != null )
						{
							methodDrawer.BindTo( typeof( ExposedMethod ), string.Empty, () => Value, ( value ) => Value = value );
							methodDrawer.SetBoundMethod( method );

							exposedMethods.Add( methodDrawer );
						}
					}
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
					elements[i].Refresh();
			}
		}

		protected InspectorField CreateDrawerForComponent( Component component, string variableName = null )
		{
			InspectorField variableDrawer = Inspector.CreateDrawerForType( component.GetType(), drawArea, Depth + 1, false );
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

		protected InspectorField CreateDrawerForVariable( MemberInfo variable, string variableName = null )
		{
			Type variableType = variable is FieldInfo ? ( (FieldInfo) variable ).FieldType : ( (PropertyInfo) variable ).PropertyType;
			InspectorField variableDrawer = Inspector.CreateDrawerForType( variableType, drawArea, Depth + 1 );
			if( variableDrawer != null )
			{
				variableDrawer.BindTo( this, variable, variableName );
				elements.Add( variableDrawer );
			}

			return variableDrawer;
		}

		protected InspectorField CreateDrawer( Type variableType, string variableName, Getter getter, Setter setter, bool drawObjectsAsFields = true )
		{
			InspectorField variableDrawer = Inspector.CreateDrawerForType( variableType, drawArea, Depth + 1, drawObjectsAsFields );
			if( variableDrawer != null )
			{
				variableDrawer.BindTo( variableType, variableName, getter, setter );
				elements.Add( variableDrawer );
			}

			return variableDrawer;
		}
	}
}