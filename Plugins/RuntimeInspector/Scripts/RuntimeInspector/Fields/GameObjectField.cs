using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

namespace RuntimeInspectorNamespace
{
	public class GameObjectField : ExpandableInspectorField
	{
		protected override int Length { get { return components.Count + 4; } } // 4: active, name, tag, layer

		private string currentTag = null;
		private StringField nameField, tagField;

		private readonly List<Component> components = new List<Component>( 8 );
		private readonly List<bool> componentsExpandedStates = new List<bool>();

		public override bool SupportsType( Type type )
		{
			return type == typeof( GameObject );
		}

		protected override void OnBound( MemberInfo variable )
		{
			base.OnBound( variable );
			currentTag = ( (GameObject) Value ).tag;
		}

		protected override void OnUnbound()
		{
			base.OnUnbound();

			components.Clear();
			componentsExpandedStates.Clear();
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

			componentsExpandedStates.Clear();
			for( int i = 0; i < elements.Count; i++ )
				componentsExpandedStates.Add( ( elements[i] is ExpandableInspectorField ) ? ( (ExpandableInspectorField) elements[i] ).IsExpanded : false );

			base.ClearElements();
		}

		protected override void GenerateElements()
		{
			if( components.Count == 0 )
				return;

			CreateDrawer( typeof( bool ), "Is Active", () => ( (GameObject) Value ).activeSelf, ( value ) => ( (GameObject) Value ).SetActive( (bool) value ) );
			nameField = CreateDrawer( typeof( string ), "Name", () => ( (GameObject) Value ).name, ( value ) =>
			{
				( (GameObject) Value ).name = (string) value;
				NameRaw = Value.GetNameWithType();

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

			for( int i = 0, j = elements.Count; i < components.Count; i++ )
			{
				InspectorField componentDrawer = CreateDrawerForComponent( components[i] );
				if( componentDrawer != null )
				{
					if( j < componentsExpandedStates.Count && componentsExpandedStates[j] && componentDrawer is ExpandableInspectorField )
						( (ExpandableInspectorField) componentDrawer ).IsExpanded = true;

					j++;
				}
			}

			if( nameField != null )
				nameField.SetterMode = StringField.Mode.OnSubmit;

			if( tagField != null )
				tagField.SetterMode = StringField.Mode.OnSubmit;

			componentsExpandedStates.Clear();
		}

		public override void Refresh()
		{
			base.Refresh();
			components.Clear();

			GameObject go = Value as GameObject;
			if( go  )
			{
				go.GetComponents( components );

				for( int i = components.Count - 1; i >= 0; i-- )
				{
					if( !components[i] )
						components.RemoveAt( i );
				}

				if( Inspector.ComponentFilter != null )
					Inspector.ComponentFilter( go, components );
			}
		}
	}
}