#if !UNITY_EDITOR && NETFX_CORE
using System.Reflection;
#endif
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RuntimeInspectorNamespace
{
	public class GameObjectField : ExpandableInspectorField
	{
		protected override int Length { get { return components.Count + 4; } }

		private string currentTag = null;
		private List<Component> components = new List<Component>( 8 );

		private StringField nameField, tagField;

		public override bool SupportsType( Type type )
		{
			return type == typeof( GameObject );
		}

		protected override void OnBound()
		{
			base.OnBound();
			currentTag = ( (GameObject) Value ).tag;
		}

		protected override void OnUnbound()
		{
			base.OnUnbound();
			components.Clear();
		}

		protected override void GenerateElements()
		{
			if( components.Count == 0 )
				return;

			CreateDrawer( typeof( bool ), "Is Active", () => ( (GameObject) Value ).activeSelf, ( value ) => ( (GameObject) Value ).SetActive( (bool) value ) );
			nameField = CreateDrawer( typeof( string ), "Name", () => ( (GameObject) Value ).name, (value) =>
			{
				( (GameObject) Value ).name = (string) value;

				RuntimeHierarchy hierarchy = Inspector.ConnectedHierarchy;
				if( hierarchy != null )
					hierarchy.RefreshNameOf( ( (GameObject) Value ).transform );
			} ) as StringField;
			tagField = CreateDrawer( typeof( string ), "Tag", () =>
			{
				GameObject go = (GameObject) Value;
				if( !go.CompareTag( currentTag ) )
					currentTag = go.tag;

				return currentTag;
			}, ( value ) => ( (GameObject) Value ).tag = (string) value ) as StringField;
			CreateDrawerForVariable( typeof( GameObject ).GetProperty( "layer" ), "Layer" );
			
			for( int i = 0; i < components.Count; i++ )
				CreateDrawerForComponent( components[i] );

			if( nameField != null )
				nameField.SetterMode = StringField.Mode.OnSubmit;

			if( tagField != null )
				tagField.SetterMode = StringField.Mode.OnSubmit;
		}

		protected override void ClearElements()
		{
			if( nameField != null )
			{
				nameField.SetterMode = StringField.Mode.OnValueChange;
				nameField = null;
			}

			if( tagField != null )
			{
				tagField.SetterMode = StringField.Mode.OnValueChange;
				tagField = null;
			}

			base.ClearElements();
		}

		public override void Refresh()
		{
			base.Refresh();

			components.Clear();
			if( !Value.IsNull() )
			{
				GameObject go = (GameObject) Value;
				go.GetComponents( components );

				for( int i = components.Count - 1; i >= 0; i-- )
				{
					if( components[i].IsNull() )
						components.RemoveAt( i );
				}
            }
		}
	}
}