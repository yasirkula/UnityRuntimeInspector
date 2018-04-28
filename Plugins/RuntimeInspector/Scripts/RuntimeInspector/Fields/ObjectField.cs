using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace RuntimeInspectorNamespace
{
	public class ObjectField : ExpandableInspectorField
	{
		[SerializeField]
		private Button initializeObjectButton;

		private bool elementsInitialized = false;

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

		public override bool SupportsType( Type type )
		{
#if UNITY_EDITOR || !NETFX_CORE
			return typeof( UnityEngine.Object ).IsAssignableFrom( type ) || Attribute.IsDefined( type, typeof( SerializableAttribute ), false );
#else
			return typeof( UnityEngine.Object ).IsAssignableFrom( type ) || type.GetTypeInfo().IsDefined( typeof( SerializableAttribute ), false );
#endif
		}

		protected override void OnBound()
		{
			elementsInitialized = false;
			base.OnBound();
		}

		protected override void GenerateElements()
		{
			if( Value.IsNull() )
			{
				initializeObjectButton.gameObject.SetActive( CanInitializeNewObject() );
				return;
			}

			initializeObjectButton.gameObject.SetActive( false );
			
			foreach( MemberInfo variables in Inspector.GetExposedVariablesForType( Value.GetType() ) )
				CreateDrawerForVariable( variables );
		}

		protected override void OnSkinChanged()
		{
			base.OnSkinChanged();
			initializeObjectButton.SetSkinButton( Skin );
		}

		private bool CanInitializeNewObject()
		{
#if UNITY_EDITOR || !NETFX_CORE
			if( BoundVariableType.IsAbstract || BoundVariableType.IsInterface )
#else
			if( BoundVariableType.GetTypeInfo().IsAbstract || BoundVariableType.GetTypeInfo().IsInterface )
#endif
				return false;

			if( typeof( ScriptableObject ).IsAssignableFrom( BoundVariableType ) )
				return true;

			if( typeof( UnityEngine.Object ).IsAssignableFrom( BoundVariableType ) )
				return false;

			if( BoundVariableType.IsArray )
				return false;

#if UNITY_EDITOR || !NETFX_CORE
			if( BoundVariableType.IsGenericType && BoundVariableType.GetGenericTypeDefinition() == typeof( List<> ) )
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
				Value = BoundVariableType.Instantiate();

				RegenerateElements();
				IsExpanded = true;
			}
		}
	}
}