using UnityEngine;

namespace RuntimeInspectorNamespace
{
	public class HierarchyItemSearchEntry : HierarchyItemTransform
	{
		protected override int ChildCount { get { return 0; } }

		protected override Transform GetChild( int index )
		{
			return null;
		}
	}
}