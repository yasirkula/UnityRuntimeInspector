using System.Collections.Generic;
using UnityEngine;

namespace RuntimeInspectorNamespace
{
	public class HierarchyDataRootPseudoScene : HierarchyDataRoot
	{
		private readonly string name;
		public override string Name { get { return name; } }
		public override int ChildCount { get { return rootObjects.Count; } }

		private readonly List<Transform> rootObjects = new List<Transform>();

		public HierarchyDataRootPseudoScene( RuntimeHierarchy hierarchy, string name ) : base( hierarchy )
		{
			this.name = name;
		}

		public void AddChild( Transform child )
		{
			if( !rootObjects.Contains( child ) )
				rootObjects.Add( child );
		}

		public void InsertChild( int index, Transform child )
		{
			index = Mathf.Clamp( index, 0, rootObjects.Count );
			rootObjects.Insert( index, child );

			// If the object was already in the list, remove the old copy from the list
			for( int i = rootObjects.Count - 1; i >= 0; i-- )
			{
				if( i != index && rootObjects[i] == child )
				{
					rootObjects.RemoveAt( i );
					return;
				}
			}
		}

		public void RemoveChild( Transform child )
		{
			rootObjects.Remove( child );
		}

		public override void RefreshContent()
		{
			for( int i = rootObjects.Count - 1; i >= 0; i-- )
			{
				if( !rootObjects[i] )
					rootObjects.RemoveAt( i );
			}
		}

		public override Transform GetChild( int index )
		{
			return rootObjects[index];
		}
	}
}