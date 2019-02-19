using System.Collections.Generic;
using UnityEngine;

namespace RuntimeInspectorNamespace
{
	public class HierarchyRootSearch : IHierarchyRootContent
	{
		public string Name { get { return reference.Name; } }
		public bool IsValid { get { return reference.IsValid; } }
		public List<GameObject> Children { get; set; }

		private readonly RuntimeHierarchy hierarch;
		private readonly IHierarchyRootContent reference;

		private string searchTerm;

		public HierarchyRootSearch( RuntimeHierarchy hierarch, IHierarchyRootContent reference )
		{
			this.hierarch = hierarch;
			this.reference = reference;
		}

		public void Refresh()
		{
			if( !hierarch.IsInSearchMode )
				return;

			Children.Clear();
			searchTerm = hierarch.SearchTerm;

			List<GameObject> referenceRoot = reference.Children;
			for( int i = 0; i < referenceRoot.Count; i++ )
			{
				GameObject obj = referenceRoot[i];
				if( obj.IsNull() )
					continue;

				if( RuntimeInspectorUtils.IgnoredSearchEntries.Contains( obj.transform ) )
					continue;

				if( obj.name.IndexOf( searchTerm, System.StringComparison.OrdinalIgnoreCase ) >= 0 )
					Children.Add( obj );

				SearchTransformRecursively( obj.transform );
			}
		}

		private void SearchTransformRecursively( Transform obj )
		{
			for( int i = 0; i < obj.childCount; i++ )
			{
				Transform child = obj.GetChild( i );
				if( RuntimeInspectorUtils.IgnoredSearchEntries.Contains( child ) )
					continue;

				if( child.name.IndexOf( searchTerm, System.StringComparison.OrdinalIgnoreCase ) >= 0 )
					Children.Add( child.gameObject );

				SearchTransformRecursively( child );
			}
		}
	}
}