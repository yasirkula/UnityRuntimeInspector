using System.Collections.Generic;
using UnityEngine;

namespace RuntimeInspectorNamespace
{
	public class HierarchyDataRootPseudoScene : HierarchyDataRoot
	{
		private readonly string name;
		public override string Name { get { return name; } }
		public override int ChildCount { get { return rootObjects.Count; } }

		internal readonly Transform rootTransform;
		private readonly List<Transform> rootObjects = new List<Transform>();

		public HierarchyDataRootPseudoScene( RuntimeHierarchy hierarchy, string name, Transform rootTransform = null ) : base( hierarchy )
		{
			this.name = name;
			this.rootTransform = rootTransform;
		}

		public void AddChild( Transform child )
		{
			if( rootTransform != null )
				child.SetParent( rootTransform, true );
			else if( !rootObjects.Contains( child ) )
				rootObjects.Add( child );
		}

		public void InsertChild( int index, Transform child )
		{
			if( rootTransform != null )
			{
				child.SetParent( rootTransform, true );
				child.SetSiblingIndex( index );
			}
			else
			{
				rootObjects.Remove( child ); // If the object was already in the list, remove the old copy from the list
				rootObjects.Insert( Mathf.Clamp( index, 0, rootObjects.Count ), child );
			}
		}

		public void RemoveChild( Transform child )
		{
			if( rootTransform == null )
				rootObjects.Remove( child );
		}

		public override void RefreshContent()
		{
			if( rootTransform != null )
			{
				rootObjects.Clear();

				for( int i = 0, childCount = rootTransform.childCount; i < childCount; i++ )
					rootObjects.Add( rootTransform.GetChild( i ) );
			}
			else
				rootObjects.RemoveAll( ( transform ) => transform == null );
		}

		public override Transform GetChild( int index )
		{
			return rootObjects[index];
		}

		public override Transform GetNearestRootOf( Transform target )
		{
			Transform result = null;
			for( int i = rootObjects.Count - 1; i >= 0; i-- )
			{
				Transform rootObject = rootObjects[i];
				if( rootObject && target.IsChildOf( rootObject ) && ( !result || rootObject.IsChildOf( result ) ) )
					result = rootObject;
			}

			return result;
		}
	}
}