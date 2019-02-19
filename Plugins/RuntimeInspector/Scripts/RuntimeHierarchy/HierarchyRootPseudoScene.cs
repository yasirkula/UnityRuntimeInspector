using System.Collections.Generic;
using UnityEngine;

namespace RuntimeInspectorNamespace
{
	public class HierarchyRootPseudoScene : IHierarchyRootContent
	{
		private readonly string name;
		public string Name { get { return name; } }
		public bool IsValid { get { return true; } }
		public List<GameObject> Children { get; set; }

		public HierarchyRootPseudoScene( string name )
		{
			this.name = name;
		}

		public void AddChild( Transform child )
		{
			if( !Children.Contains( child.gameObject ) )
				Children.Add( child.gameObject );
		}

		public void RemoveChild( Transform child )
		{
			Children.Remove( child.gameObject );
		}

		public void Refresh()
		{
			for( int i = Children.Count - 1; i >= 0; i-- )
			{
				if( Children[i].IsNull() )
					Children.RemoveAt( i );
			}
		}
	}
}