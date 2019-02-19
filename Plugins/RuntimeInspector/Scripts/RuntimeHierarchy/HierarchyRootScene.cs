using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RuntimeInspectorNamespace
{
	public class HierarchyRootScene : IHierarchyRootContent
	{
		public string Name { get { return Scene.name; } }
		public bool IsValid { get { return Scene.IsValid(); } }
		public List<GameObject> Children { get; set; }

		public Scene Scene { get; private set; }

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