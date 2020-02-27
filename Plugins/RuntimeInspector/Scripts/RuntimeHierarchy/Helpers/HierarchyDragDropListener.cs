//#define CAN_ADD_OBJECTS_TO_PSEUDO_SCENES
//#define CAN_DROP_PARENT_ON_CHILD

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace RuntimeInspectorNamespace
{
	public class HierarchyDragDropListener : MonoBehaviour, IDropHandler
	{
		private HierarchyField hierarchyItem;

		private RuntimeHierarchy m_hierarchy;
		private RuntimeHierarchy Hierarchy
		{
			get
			{
				if( m_hierarchy )
					return m_hierarchy;

				if( hierarchyItem )
					return hierarchyItem.Hierarchy;

				return GetComponentInParent<RuntimeHierarchy>();
			}
		}

		private void Awake()
		{
			hierarchyItem = GetComponent<HierarchyField>();
			if( !hierarchyItem )
				m_hierarchy = GetComponent<RuntimeHierarchy>();
		}

		public void OnDrop( PointerEventData eventData )
		{
			RuntimeHierarchy hierarchy = Hierarchy;
			if( !hierarchy || !hierarchy.CanReorganizeItems )
				return;

			Transform droppedTransform = RuntimeInspectorUtils.GetAssignableObjectFromDraggedReferenceItem( eventData, typeof( Transform ) ) as Transform;
			if( !droppedTransform )
				return;

			if( !hierarchyItem )
			{
				if( droppedTransform.parent == null )
					return;

				droppedTransform.SetParent( null, true );
			}
			else if( hierarchyItem.Data is HierarchyDataTransform )
			{
				Transform newParent = hierarchyItem.Data.BoundTransform;
				if( droppedTransform.parent == newParent || droppedTransform == newParent )
					return;

				// Avoid setting child object as parent of the parent object
#if !CAN_DROP_PARENT_ON_CHILD
				if( newParent.IsChildOf( droppedTransform ) )
					return;
#else
				Transform curr = newParent;
				while( curr.parent != null && curr.parent != droppedTransform )
					curr = curr.parent;

				if( curr.parent == droppedTransform )
					curr.SetParent( droppedTransform.parent, true );
#endif

				droppedTransform.SetParent( newParent, true );
			}
			else
			{
				HierarchyDataRoot rootData = (HierarchyDataRoot) hierarchyItem.Data;
				if( rootData is HierarchyDataRootPseudoScene )
				{
#if CAN_ADD_OBJECTS_TO_PSEUDO_SCENES
					( (HierarchyDataRootPseudoScene) rootData ).AddChild( droppedTransform ); // Add object to pseudo-scene
#else
					return;
#endif
				}
				else if( rootData is HierarchyDataRootScene )
				{
					bool parentChanged = false;
					if( droppedTransform.parent != null )
					{
						droppedTransform.SetParent( null, true );
						parentChanged = true;
					}

					Scene scene = ( (HierarchyDataRootScene) rootData ).Scene;
					if( droppedTransform.gameObject.scene != scene )
					{
						SceneManager.MoveGameObjectToScene( droppedTransform.gameObject, scene );
						parentChanged = true;
					}

					if( !parentChanged )
						return;
				}
			}

			hierarchy.Select( droppedTransform, true );
		}
	}
}