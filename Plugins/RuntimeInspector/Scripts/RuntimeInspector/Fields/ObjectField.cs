using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace RuntimeInspectorNamespace
{
	public class ObjectField : ExpandableInspectorField<object>
	{
#pragma warning disable 0649
		[SerializeField]
		private Button initializeObjectButton;
#pragma warning restore 0649

		private bool elementsInitialized = false;
		private IRuntimeInspectorCustomEditor customEditor;

		protected override int Length
		{
			get
			{
				if( Value.IsNull() )
				{
					if( !initializeObjectButton.gameObject.activeSelf )
						return -1;

					return 0;
				}

				if( initializeObjectButton.gameObject.activeSelf )
					return -1;

				if( !elementsInitialized )
				{
					elementsInitialized = true;
					return -1;
				}

				return elements.Count;
			}
		}

		public override void Initialize()
		{
			base.Initialize();
			initializeObjectButton.onClick.AddListener( InitializeObject );
		}

		protected override void OnBound( MemberInfo variable )
		{
			elementsInitialized = false;
			base.OnBound( variable );
		}

		protected override void GenerateElements()
		{
			if( Value.IsNull() )
			{
				initializeObjectButton.gameObject.SetActive( CanInitializeNewObject() );
				return;
			}

			initializeObjectButton.gameObject.SetActive( false );

			if( ( customEditor = RuntimeInspectorUtils.GetCustomEditor( Value.GetType() ) ) != null )
				customEditor.GenerateElements( this );
			else
				CreateDrawersForVariables();
		}

		protected override void ClearElements()
		{
			base.ClearElements();

			if( customEditor != null )
			{
				customEditor.Cleanup();
				customEditor = null;
			}
		}

		protected override void OnSkinChanged()
		{
			base.OnSkinChanged();
			initializeObjectButton.SetSkinButton( Skin );
		}

		public override void Refresh()
		{
			base.Refresh();

			if( customEditor != null )
				customEditor.Refresh();
		}

		public void CreateDrawersForVariables( params string[] variables )
		{
			if( variables == null || variables.Length == 0 )
			{
				foreach( MemberInfo variable in Inspector.GetExposedVariablesForType( Value.GetType() ) )
				{
						CreateDrawerForVariable<object>( variable );
				}
			}
			else
			{
				foreach( MemberInfo variable in Inspector.GetExposedVariablesForType( Value.GetType() ) )
				{
					if( Array.IndexOf( variables, variable.Name ) >= 0 )
						CreateDrawerForVariable<object>( variable );
				}
			}
		}

		public void CreateDrawersForVariablesExcluding( params string[] variablesToExclude )
		{
			if( variablesToExclude == null || variablesToExclude.Length == 0 )
			{
				foreach( MemberInfo variable in Inspector.GetExposedVariablesForType( Value.GetType() ) )
					CreateDrawerForVariable<object>( variable );
			}
			else
			{
				foreach( MemberInfo variable in Inspector.GetExposedVariablesForType( Value.GetType() ) )
				{
					if( Array.IndexOf( variablesToExclude, variable.Name ) < 0 )
						CreateDrawerForVariable<object>( variable );
				}
			}
		}

		private bool CanInitializeNewObject()
		{
#if UNITY_EDITOR || !NETFX_CORE
			if( m_boundVariableType.IsAbstract || m_boundVariableType.IsInterface )
#else
			if( BoundVariableType.GetTypeInfo().IsAbstract || BoundVariableType.GetTypeInfo().IsInterface )
#endif
				return false;

			if( typeof( ScriptableObject ).IsAssignableFrom( m_boundVariableType ) )
				return true;

			if( typeof( UnityEngine.Object ).IsAssignableFrom( m_boundVariableType ) )
				return false;

			if( m_boundVariableType.IsArray )
				return false;

#if UNITY_EDITOR || !NETFX_CORE
			if( m_boundVariableType.IsGenericType && m_boundVariableType.GetGenericTypeDefinition() == typeof( List<> ) )
#else
			if( BoundVariableType.GetTypeInfo().IsGenericType && BoundVariableType.GetGenericTypeDefinition() == typeof( List<> ) )
#endif
				return false;

			return true;
		}

		private void InitializeObject()
		{
			if( CanInitializeNewObject() )
			{
				Value = m_boundVariableType.Instantiate();

				RegenerateElements();
				IsExpanded = true;
			}
		}
	}
}
