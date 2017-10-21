using System.Collections.Generic;
using UnityEngine;

namespace RuntimeInspectorNamespace
{
	public interface IHierarchyRootContent
	{
		bool IsValid { get; }
		int ChildCount { get; }
		string Name { get; }
		
		List<GameObject> Children { get; set; }

		void Refresh();
	}
}