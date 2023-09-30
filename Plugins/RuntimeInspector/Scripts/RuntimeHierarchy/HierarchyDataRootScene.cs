using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RuntimeInspectorNamespace
{
	public class HierarchyDataRootScene : HierarchyDataRoot
	{
		public override string Name { get { return Scene.name; } }
		public override int ChildCount { get { return rootObjects.Count; } }

		public Scene Scene { get; private set; }

		private readonly List<GameObject> rootObjects = new List<GameObject>();

		public HierarchyDataRootScene( RuntimeHierarchy hierarchy, Scene target ) : base( hierarchy )
		{
			Scene = target;
		}

		public override void RefreshContent()
		{
			rootObjects.Clear();

			if( Scene.isLoaded )
				Scene.GetRootGameObjects( rootObjects );
		}

		public override Transform GetChild( int index )
		{
			GameObject rootObject = rootObjects[index];
			return rootObject ? rootObject.transform : null;
		}

		public override Transform GetNearestRootOf( Transform target )
		{
			return ( target.gameObject.scene == Scene ) ? target.root : null;
		}
	}
}