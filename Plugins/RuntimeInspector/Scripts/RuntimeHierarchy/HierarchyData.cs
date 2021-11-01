using System.Collections.Generic;
using UnityEngine;

namespace RuntimeInspectorNamespace
{
	public abstract class HierarchyData
	{
		private static readonly List<HierarchyDataTransform> transformDataPool = new List<HierarchyDataTransform>( 32 );
		private static readonly List<List<HierarchyDataTransform>> childrenListPool = new List<List<HierarchyDataTransform>>( 32 );

		public abstract string Name { get; }
		public abstract bool IsActive { get; }
		public abstract int ChildCount { get; }
		public abstract Transform BoundTransform { get; }

		protected List<HierarchyDataTransform> children;
		protected HierarchyData parent;
		public HierarchyDataRoot Root
		{
			get
			{
				HierarchyData _parent = this;
				while( _parent.parent != null )
					_parent = _parent.parent;

				return (HierarchyDataRoot) _parent;
			}
		}

		protected int m_index;
		public int Index { get { return m_index; } }
		public int AbsoluteIndex
		{
			get
			{
				int result = m_index;
				HierarchyData _parent = parent;
				while( _parent != null )
				{
					result += _parent.m_index + 1;
					_parent = _parent.parent;
				}

				return result;
			}
		}

		protected int m_height = 1;
		public int Height { get { return m_height; } }

		protected int m_depth;
		public int Depth { get { return m_depth; } }

		public bool CanExpand { get { return ChildCount > 0; } }
		public bool IsExpanded
		{
			get { return children != null; }
			set
			{
				if( IsExpanded == value )
					return;

				if( value )
				{
					if( ChildCount == 0 )
						return;

					PopChildrenList();
				}
				else
					PoolChildrenList();

				int prevHeight = m_height;
				Refresh();

				int deltaHeight = m_height - prevHeight;
				if( deltaHeight != 0 )
				{
					if( parent != null )
					{
						HierarchyData child = this;
						HierarchyData _parent = parent;
						while( _parent != null )
						{
							List<HierarchyDataTransform> children = _parent.children;
							for( int i = children.IndexOf( (HierarchyDataTransform) child ) + 1, childCount = children.Count; i < childCount; i++ )
								children[i].m_index += deltaHeight;

							_parent.m_height += deltaHeight;

							child = _parent;
							_parent = _parent.parent;
						}
					}

					HierarchyDataRoot root = Root;
					if( root != null )
						root.Hierarchy.SetListViewDirty();
				}
			}
		}

		public virtual bool Refresh()
		{
			if( m_depth < 0 ) // This object is hidden from Hierarchy
				return false;

			m_height = 1;
			bool hasChanged = false;
			int childCount = ChildCount;

			if( IsExpanded )
			{
				if( childCount != children.Count )
					hasChanged = true;

				//if( childCount == 0 ) // Issue with IsExpanded's Refresh changing iteratedIndex
				//	PoolChildrenList();
				//else
				{
					RuntimeHierarchy hierarchy = null; // Root's RuntimeHierarchy will be fetched only once when it is needed
					for( int i = 0; i < childCount; i++ )
					{
						Transform child = GetChild( i );
						if( children.Count <= i )
						{
							if( hierarchy == null )
								hierarchy = Root.Hierarchy;

							GenerateChildItem( child, i, hierarchy );
						}
						else if( children[i].BoundTransform != child )
						{
							int childIndex;
							for( childIndex = 0; childIndex < children.Count; childIndex++ )
							{
								if( children[childIndex].BoundTransform == child )
									break;
							}

							if( childIndex == children.Count )
							{
								if( hierarchy == null )
									hierarchy = Root.Hierarchy;

								GenerateChildItem( child, i, hierarchy );
							}
							else
							{
								HierarchyDataTransform childItem = children[childIndex];
								children.RemoveAt( childIndex );
								children.Insert( i, childItem );
							}

							hasChanged = true;
						}

						hasChanged |= children[i].Refresh();
						children[i].m_index = m_height - 1;
						m_height += children[i].m_height;
					}

					for( int i = children.Count - 1; i >= childCount; i-- )
						RemoveChildItem( i );
				}
			}

			return hasChanged;
		}

		public HierarchyData FindDataAtIndex( int index )
		{
			int upperBound = children.Count - 1;
			if( index <= upperBound && children[index].m_index == index )
			{
				int middle = index;
				while( middle < upperBound && index == children[middle + 1].m_index )
					middle++;

				return children[middle];
			}

			// Binary search
			int min = 0;
			int max = upperBound;
			while( min <= max )
			{
				int middle = ( min + max ) / 2;
				int childIndex = children[middle].m_index;
				if( index == childIndex )
				{
					// Items hidden from the Hierarchy have same indices with their adjacent items
					while( middle < upperBound && index == children[middle + 1].m_index )
						middle++;

					return children[middle];
				}

				if( index < childIndex )
					max = middle - 1;
				else
					min = middle + 1;
			}

			if( max < 0 )
				max = 0;

			while( max < upperBound && index >= children[max + 1].m_index )
				max++;

			return children[max].FindDataAtIndex( index - 1 - children[max].m_index );
		}

		public HierarchyDataTransform FindTransform( Transform target, Transform nextInPath = null )
		{
			if( m_depth < 0 ) // This object is hidden from Hierarchy
				return null;

			bool isInitSearch = nextInPath == null;
			if( isInitSearch )
			{
				nextInPath = ( this is HierarchyDataRootSearch ) ? target : target.root;

				// In the current implementation, FindTransform is only called from RuntimeHierarchy.Select which
				// automatically calls RefreshContent prior to FindTransform
				//( (HierarchyDataRoot) this ).RefreshContent();
			}

			int childIndex = IndexOf( nextInPath );
			if( childIndex < 0 )
			{
				if( isInitSearch && this is HierarchyDataRootPseudoScene )
				{
					nextInPath = target;
					childIndex = IndexOf( nextInPath );
					while( childIndex < 0 && nextInPath != null )
					{
						nextInPath = nextInPath.parent;
						childIndex = IndexOf( nextInPath );
					}

					if( childIndex < 0 )
						return null;
				}
				else
					return null;
			}

			if( !CanExpand )
				return null;

			bool wasExpanded = IsExpanded;
			if( !wasExpanded )
				IsExpanded = true;

			HierarchyDataTransform childItem = children[childIndex];
			if( childItem.BoundTransform == target )
				return childItem;

			HierarchyDataTransform result = null;
			if( childItem.BoundTransform == nextInPath )
			{
				Transform next = target;
				Transform parent = next.parent;
				while( parent != null && parent != nextInPath )
				{
					next = parent;
					parent = next.parent;
				}

				if( parent != null )
					result = childItem.FindTransform( target, next );
			}

			if( result != null && result.m_depth < 0 )
				result = null;

			if( result == null && !wasExpanded )
				IsExpanded = false;

			return result;
		}

		public virtual HierarchyDataTransform FindTransformInVisibleChildren( Transform target, int targetDepth = -1 )
		{
			for( int i = 0; i < children.Count; i++ )
			{
				HierarchyDataTransform child = children[i];
				if( child.m_depth < 0 )
					continue;

				if( ReferenceEquals( child.BoundTransform, target ) )
				{
					if( targetDepth <= 0 || child.m_depth == targetDepth )
						return child;
				}
				else if( ( targetDepth <= 0 || child.m_depth < targetDepth ) && child.IsExpanded && child.BoundTransform && target.IsChildOf( child.BoundTransform ) )
				{
					child = child.FindTransformInVisibleChildren( target, targetDepth );
					if( child != null )
						return child;
				}
			}

			return null;
		}

		public abstract Transform GetChild( int index );

		public int IndexOf( Transform transform )
		{
			for( int i = ChildCount - 1; i >= 0; i-- )
			{
				if( ReferenceEquals( GetChild( i ), transform ) )
					return i;
			}

			return -1;
		}

		public void GetSiblingIndexTraversalList( List<int> traversalList )
		{
			traversalList.Clear();

			HierarchyData current = this;
			while( current.parent != null )
			{
				traversalList.Add( current.parent.children.IndexOf( (HierarchyDataTransform) current ) );
				current = current.parent;
			}
		}

		public HierarchyData TraverseSiblingIndexList( List<int> traversalList )
		{
			HierarchyData current = this;
			for( int i = traversalList.Count - 1; i >= 0; i-- )
			{
				int siblingIndex = traversalList[i];
				if( current.children == null || siblingIndex >= current.children.Count )
					return null;

				current = current.children[siblingIndex];
			}

			return current;
		}

		private void GenerateChildItem( Transform child, int index, RuntimeHierarchy hierarchy )
		{
			bool isChildVisible = !RuntimeInspectorUtils.IgnoredTransformsInHierarchy.Contains( child );
			if( isChildVisible && hierarchy.GameObjectFilter != null )
				isChildVisible = hierarchy.GameObjectFilter( child );

			HierarchyDataTransform childData;
			int poolIndex = transformDataPool.Count - 1;
			if( poolIndex >= 0 )
			{
				childData = transformDataPool[poolIndex];
				transformDataPool.RemoveAt( poolIndex );
			}
			else
				childData = new HierarchyDataTransform();

			childData.Initialize( child, this is HierarchyDataRootSearch );
			childData.parent = this;
			if( isChildVisible )
			{
				childData.m_depth = m_depth + 1;
				childData.m_height = 1;
			}
			else
			{
				childData.m_depth = -1;
				childData.m_height = 0;
			}

			children.Insert( index, childData );
		}

		private void RemoveChildItem( int index )
		{
			children[index].PoolData();
			transformDataPool.Add( children[index] );
			children.RemoveAt( index );
		}

		protected void PoolChildrenList()
		{
			if( children != null )
			{
				for( int i = children.Count - 1; i >= 0; i-- )
				{
					children[i].PoolData();
					transformDataPool.Add( children[i] );
				}

				children.Clear();
				childrenListPool.Add( children );
				children = null;
			}
		}

		protected void PopChildrenList()
		{
			int desiredSize = ChildCount;
			int bestFittingListIndex = -1;
			int bestFittingListDeltaSize = int.MaxValue;
			for( int i = childrenListPool.Count - 1; i >= 0; i-- )
			{
				int deltaSize = childrenListPool[i].Capacity - desiredSize;
				if( deltaSize < 0 )
					deltaSize = -deltaSize;

				if( deltaSize < bestFittingListDeltaSize )
				{
					bestFittingListDeltaSize = deltaSize;
					bestFittingListIndex = i;
				}
			}

			if( bestFittingListIndex >= 0 )
			{
				children = childrenListPool[bestFittingListIndex];
				childrenListPool.RemoveAt( bestFittingListIndex );
			}
			else
				children = new List<HierarchyDataTransform>( ChildCount );
		}

		public static void ClearPool()
		{
			childrenListPool.Clear();
			transformDataPool.Clear();

			if( childrenListPool.Capacity > 128 )
				childrenListPool.Capacity = 128;
			if( transformDataPool.Capacity > 128 )
				transformDataPool.Capacity = 128;
		}
	}
}