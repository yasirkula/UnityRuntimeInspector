using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RuntimeInspectorNamespace
{
	public class HierarchyRootScene : IHierarchyRootContent
	{
		public bool IsValid { get { return Scene.IsValid(); } }
		public int ChildCount { get { return Children.Count; } }
		public string Name { get { return Scene.name; } }

		public Scene Scene { get; private set; }
		public List<GameObject> Children { get; set; }

		public HierarchyRootScene( Scene target )
		{
			Scene = target;
		}

		public void Refresh()
		{
			Children.Clear();
			Scene.GetRootGameObjects( Children );
		}
	}
}