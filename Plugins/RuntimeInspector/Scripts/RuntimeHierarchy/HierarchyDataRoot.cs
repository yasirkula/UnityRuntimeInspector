using UnityEngine;

namespace RuntimeInspectorNamespace
{
	public abstract class HierarchyDataRoot : HierarchyData
	{
		public override Transform BoundTransform { get { return null; } }
		public override bool IsActive { get { return true; } }

		public RuntimeHierarchy Hierarchy { get; private set; }

		protected HierarchyDataRoot( RuntimeHierarchy hierarchy )
		{
			Hierarchy = hierarchy;

			// Root data are expanded by default
			PopChildrenList();
		}

		public abstract Transform GetNearestRootOf( Transform target );
		public abstract void RefreshContent();

		public override bool Refresh()
		{
			RefreshContent();
			return base.Refresh();
		}

		public override HierarchyDataTransform FindTransformInVisibleChildren( Transform target, int targetDepth = -1 )
		{
			return ( m_depth >= 0 && IsExpanded ) ? base.FindTransformInVisibleChildren( target, targetDepth ) : null;
		}

		public void ResetCachedNames()
		{
			if( children != null )
			{
				for( int i = children.Count - 1; i >= 0; i-- )
					children[i].ResetCachedName();
			}
		}

		public void RefreshNameOf( Transform target )
		{
			if( children != null )
			{
				for( int i = children.Count - 1; i >= 0; i-- )
					children[i].RefreshNameOf( target );
			}
		}
	}
}