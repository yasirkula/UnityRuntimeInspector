using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace RuntimeInspectorNamespace
{
	public class HierarchyDataRootSearch : HierarchyDataRoot
	{
		public override string Name { get { return reference.Name; } }
		public override int ChildCount { get { return searchResult.Count; } }
		internal Transform RootTransform { get { return ( reference is HierarchyDataRootPseudoScene ) ? ( reference as HierarchyDataRootPseudoScene ).rootTransform : null; } }

		private readonly List<Transform> searchResult = new List<Transform>();

		private readonly HierarchyDataRoot reference;

		private string searchTerm;

		public HierarchyDataRootSearch( RuntimeHierarchy hierarchy, HierarchyDataRoot reference ) : base( hierarchy )
		{
			this.reference = reference;
		}

		public override void RefreshContent()
		{
			if( !Hierarchy.IsInSearchMode )
				return;

			searchResult.Clear();
			searchTerm = Hierarchy.SearchTerm;

			for( int i = 0, childCount = reference.ChildCount; i < childCount; i++ )
			{
				Transform obj = reference.GetChild( i );
				if( obj != null )
					SearchTransformRecursively( obj );
			}
		}

		public override bool Refresh()
		{
			m_depth = 0;
			bool result = base.Refresh();

			// Scenes with no matching search results should be hidden in search mode
			if( searchResult.Count == 0 )
			{
				m_height = 0;
				m_depth = -1;
			}

			return result;
		}

		public override HierarchyDataTransform FindTransformInVisibleChildren( Transform target, int targetDepth = -1 )
		{
			if( m_depth < 0 || targetDepth > 1 || !IsExpanded )
				return null;

			for( int i = children.Count - 1; i >= 0; i-- )
			{
				if( ReferenceEquals( children[i].BoundTransform, target ) )
					return children[i];
			}

			return null;
		}

		private void SearchTransformRecursively( Transform obj )
		{
			if( RuntimeInspectorUtils.IgnoredTransformsInHierarchy.Contains( obj ) )
				return;

			if( RuntimeInspectorUtils.caseInsensitiveComparer.IndexOf( obj.name, searchTerm, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace ) >= 0 )
				searchResult.Add( obj );

			for( int i = 0, childCount = obj.childCount; i < childCount; i++ )
				SearchTransformRecursively( obj.GetChild( i ) );
		}

		public override Transform GetChild( int index )
		{
			return searchResult[index];
		}

		public override Transform GetNearestRootOf( Transform target )
		{
			return searchResult.Contains( target ) ? target : null;
		}
	}
}