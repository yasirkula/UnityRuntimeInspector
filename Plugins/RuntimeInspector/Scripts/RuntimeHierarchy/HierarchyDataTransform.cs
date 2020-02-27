using UnityEngine;

namespace RuntimeInspectorNamespace
{
	public class HierarchyDataTransform : HierarchyData
	{
		private string cachedName;
		public override string Name
		{
			get
			{
				if( cachedName == null )
					cachedName = transform ? transform.name : "<destroyed>";

				return cachedName;
			}
		}

		public override int ChildCount { get { return ( !isSearchEntry && transform ) ? transform.childCount : 0; } }
		public override Transform BoundTransform { get { return transform; } }
		public override bool IsActive { get { return transform ? transform.gameObject.activeInHierarchy : true; } }

		private Transform transform;
		private bool isSearchEntry;

		public void Initialize( Transform transform, bool isSearchEntry )
		{
			this.transform = transform;
			this.isSearchEntry = isSearchEntry;
		}

		public override Transform GetChild( int index )
		{
			return transform.GetChild( index );
		}

		public void ResetCachedName()
		{
			cachedName = null;

			if( children != null )
			{
				for( int i = children.Count - 1; i >= 0; i-- )
					children[i].ResetCachedName();
			}
		}

		public void RefreshNameOf( Transform target )
		{
			if( ReferenceEquals( transform, target ) )
				cachedName = target.name;
			else if( children != null )
			{
				for( int i = children.Count - 1; i >= 0; i-- )
					children[i].RefreshNameOf( target );
			}
		}

		public void PoolData()
		{
			parent = null;
			cachedName = null;
			m_depth = 0;
			m_height = 0;

			PoolChildrenList();
		}
	}
}