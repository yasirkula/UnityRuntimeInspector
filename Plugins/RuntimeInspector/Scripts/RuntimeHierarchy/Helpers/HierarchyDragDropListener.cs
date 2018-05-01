using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace RuntimeInspectorNamespace
{
	public class HierarchyDragDropListener : MonoBehaviour, IDropHandler
	{
		private HierarchyItem hierarchyItem;

		private RuntimeHierarchy m_hierarchy;
		private RuntimeHierarchy Hierarchy
		{
			get
			{
				if( m_hierarchy != null )
					return m_hierarchy;

				if( hierarchyItem != null )
					return hierarchyItem.Hierarchy;

				return GetComponentInParent<RuntimeHierarchy>();
			}
		}

		private void Awake()
		{
			hierarchyItem = GetComponent<HierarchyItem>();
			if( hierarchyItem == null )
				m_hierarchy = GetComponent<RuntimeHierarchy>();
		}

		public void OnDrop( PointerEventData eventData )
		{
			RuntimeHierarchy hierarchy = Hierarchy;
			if( hierarchy == null || !hierarchy.CanReorganizeItems )
				return;

			Transform droppedTransform = RuntimeInspectorUtils.GetAssignableObjectFromDraggedReferenceItem( eventData, typeof( Transform ) ) as Transform;
			if( droppedTransform == null )
				return;

			if( hierarchyItem == null )
			{
				if( droppedTransform.parent == null )
					return;

				droppedTransform.SetParent( null, true );
			}
			else if( hierarchyItem is HierarchyItemTransform )
			{
				Transform newParent = ( (HierarchyItemTransform) hierarchyItem ).BoundTransform;
				if( droppedTransform.parent == newParent || droppedTransform == newParent )
					return;

				// Avoid setting child object as parent of the parent object
				Transform curr = newParent;
				while( curr.parent != null && curr.parent != droppedTransform )
					curr = curr.parent;

				if( curr.parent == droppedTransform )
					curr.SetParent( droppedTransform.parent, true );

				droppedTransform.SetParent( newParent, true );
			}
			else
			{
				IHierarchyRootContent rootContent = ( (HierarchyItemRoot) hierarchyItem ).Content;
				if( rootContent is HierarchyRootPseudoScene )
				{
					//( (HierarchyRootPseudoScene) rootContent ).AddChild( droppedTransform ); // Add object to pseudo-scene
					return;
				}
				else if( rootContent is HierarchyRootScene )
				{
					bool parentChanged = false;
					if( droppedTransform.parent != null )
					{
						droppedTransform.SetParent( null, true );
						parentChanged = true;
					}

					Scene scene = ( (HierarchyRootScene) rootContent ).Scene;
					if( droppedTransform.gameObject.scene != scene )
					{
						SceneManager.MoveGameObjectToScene( droppedTransform.gameObject, scene );
						parentChanged = true;
					}

					if( !parentChanged )
						return;
				}
			}

			if( hierarchyItem != null && !hierarchyItem.IsExpanded )
				hierarchyItem.IsExpanded = true;

			hierarchy.Refresh();
		}
	}
}