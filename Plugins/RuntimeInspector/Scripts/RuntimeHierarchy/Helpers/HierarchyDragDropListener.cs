using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RuntimeInspectorNamespace
{
	public class HierarchyDragDropListener : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
	{
		private const float POINTER_VALIDATE_INTERVAL = 5f;

#pragma warning disable 0649
		[SerializeField]
		private float siblingIndexModificationArea = 5f;

		[SerializeField]
		private float scrollableArea = 75f;
		private float _1OverScrollableArea;

		[SerializeField]
		private float scrollSpeed = 75f;

		[Header( "Internal Variables" )]
		[SerializeField]
		private RuntimeHierarchy hierarchy;

		[SerializeField]
		private RectTransform content;

		[SerializeField]
		private Image dragDropTargetVisualization;
#pragma warning restore 0649

		private Canvas canvas;

		private RectTransform rectTransform;
		private float height;

		private PointerEventData pointer;
		private Camera worldCamera;

		private float pointerLastYPos;
		private float nextPointerValidation;

		private void Start()
		{
			rectTransform = (RectTransform) transform;
			canvas = hierarchy.GetComponentInParent<Canvas>();
			_1OverScrollableArea = 1f / scrollableArea;
		}

		private void OnRectTransformDimensionsChange()
		{
			height = 0f;
		}

		private void Update()
		{
			if( pointer == null )
				return;

			nextPointerValidation -= Time.unscaledDeltaTime;
			if( nextPointerValidation <= 0f )
			{
				nextPointerValidation = POINTER_VALIDATE_INTERVAL;

				if( !pointer.IsPointerValid() )
				{
					pointer = null;
					return;
				}
			}

			Vector2 position;
			if( RectTransformUtility.ScreenPointToLocalPointInRectangle( rectTransform, pointer.position, worldCamera, out position ) && ( position.y != -pointerLastYPos || hierarchy.AutoScrollSpeed != 0f ) )
			{
				pointerLastYPos = -position.y;

				if( height <= 0f )
					height = rectTransform.rect.height;

				// Scroll the hierarchy when hovering near top or bottom edges
				float scrollAmount = 0f;
				float viewportYPos = pointerLastYPos;
				if( pointerLastYPos < scrollableArea )
					scrollAmount = ( scrollableArea - pointerLastYPos ) * _1OverScrollableArea;
				else if( pointerLastYPos > height - scrollableArea )
					scrollAmount = ( height - scrollableArea - viewportYPos ) * _1OverScrollableArea;

				float contentYPos = pointerLastYPos + content.anchoredPosition.y;
				if( contentYPos < 0f )
				{
					if( dragDropTargetVisualization.gameObject.activeSelf )
						dragDropTargetVisualization.gameObject.SetActive( false );

					hierarchy.AutoScrollSpeed = 0f;
				}
				else
				{
					if( contentYPos < hierarchy.ItemCount * hierarchy.Skin.LineHeight )
					{
						// Show a visual feedback of where the dragged object would be dropped to
						if( !dragDropTargetVisualization.gameObject.activeSelf )
						{
							dragDropTargetVisualization.rectTransform.SetAsLastSibling();
							dragDropTargetVisualization.gameObject.SetActive( true );
						}

						float relativePosition = contentYPos % hierarchy.Skin.LineHeight;
						float absolutePosition = -contentYPos + relativePosition;
						if( relativePosition < siblingIndexModificationArea )
						{
							// Dragged object will be dropped above the target
							dragDropTargetVisualization.rectTransform.anchoredPosition = new Vector2( 0f, absolutePosition + 2f );
							dragDropTargetVisualization.rectTransform.sizeDelta = new Vector2( 20f, 4f ); // 20f: The visualization extends beyond scrollbar
						}
						else if( relativePosition > hierarchy.Skin.LineHeight - siblingIndexModificationArea )
						{
							// Dragged object will be dropped below the target
							dragDropTargetVisualization.rectTransform.anchoredPosition = new Vector2( 0f, absolutePosition - hierarchy.Skin.LineHeight + 2f );
							dragDropTargetVisualization.rectTransform.sizeDelta = new Vector2( 20f, 4f );
						}
						else
						{
							// Dragged object will be dropped onto the target
							dragDropTargetVisualization.rectTransform.anchoredPosition = new Vector2( 0f, absolutePosition );
							dragDropTargetVisualization.rectTransform.sizeDelta = new Vector2( 20f, hierarchy.Skin.LineHeight );
						}
					}
					else if( dragDropTargetVisualization.gameObject.activeSelf )
						dragDropTargetVisualization.gameObject.SetActive( false );

					hierarchy.AutoScrollSpeed = scrollAmount * scrollSpeed;
				}
			}
		}

		void IDropHandler.OnDrop( PointerEventData eventData )
		{
			( (IPointerExitHandler) this ).OnPointerExit( eventData );

			if( !hierarchy.CanReorganizeItems || hierarchy.IsInSearchMode )
				return;

			Transform[] droppedTransforms = RuntimeInspectorUtils.GetAssignableObjectsFromDraggedReferenceItem<Transform>( eventData );
			if( droppedTransforms == null || droppedTransforms.Length == 0 )
				return;

			// Sorting the selection is necessary to preserve the sibling index order of the dragged Transforms
			if( droppedTransforms.Length > 1 )
				System.Array.Sort( droppedTransforms, ( transform1, transform2 ) => CompareHierarchySiblingIndices( transform1, transform2 ) );

			bool shouldFocusObjectInHierarchy = false;

			float contentYPos = pointerLastYPos + content.anchoredPosition.y;
			int dataIndex = (int) contentYPos / hierarchy.Skin.LineHeight;
			HierarchyData target = hierarchy.GetDataAt( dataIndex );
			if( target == null )
			{
				// Dropped Transform(s) onto the blank space at the bottom of the Hierarchy
				for( int i = 0; i < droppedTransforms.Length; i++ )
				{
					if( droppedTransforms[i].parent != null )
					{
						droppedTransforms[i].SetParent( null, true );
						shouldFocusObjectInHierarchy = true;
					}
				}

				if( !shouldFocusObjectInHierarchy )
					return;
			}
			else
			{
				int insertDirection;
				float relativePosition = contentYPos % hierarchy.Skin.LineHeight;
				if( relativePosition < siblingIndexModificationArea )
					insertDirection = -1;
				else if( relativePosition > hierarchy.Skin.LineHeight - siblingIndexModificationArea )
					insertDirection = 1;
				else
					insertDirection = 0;

				// Inserting above/below a scene or pseudo-scene is a special case
				if( insertDirection != 0 && !( target is HierarchyDataTransform ) )
				{
					if( insertDirection < 0 && dataIndex > 0 )
					{
						// In an hierarchy with consecutive items A and B, insert below A instead of inserting above B because it makes calculations easier
						HierarchyData _target = hierarchy.GetDataAt( dataIndex - 1 );
						if( _target != null )
						{
							target = _target;
							insertDirection = 1;
						}
					}
					else if( insertDirection > 0 && dataIndex < hierarchy.ItemCount - 1 )
					{
						// In an hierarchy with consecutive items A and B where B is a Transform, insert above B instead of inserting below A because it makes calculations easier
						HierarchyData _target = hierarchy.GetDataAt( dataIndex + 1 );
						if( _target != null && _target is HierarchyDataTransform )
						{
							target = _target;
							insertDirection = -1;
						}
					}
				}

				HierarchyDataRoot newScene = null;
				Transform newParent = null;
				int newSiblingIndex = -1;
				if( !( target is HierarchyDataTransform ) )
				{
					// Dropped onto a scene or pseudo-scene
					newScene = (HierarchyDataRoot) target;
				}
				else
				{
					// Dropped onto a Transform
					newParent = ( (HierarchyDataTransform) target ).BoundTransform;
					if( !newParent )
						return;

					if( insertDirection != 0 )
					{
						if( insertDirection > 0 && target.Height > 1 )
						{
							// Dropped below an expanded Transform, make dropped object a child of it
							newSiblingIndex = 0;
						}
						else if( target.Depth == 1 && target.Root is HierarchyDataRootPseudoScene )
						{
							// Dropped above or below a root pseudo-scene object, don't actually change the parent
							if( insertDirection < 0 )
								newSiblingIndex = ( (HierarchyDataRootPseudoScene) target.Root ).IndexOf( newParent );
							else
								newSiblingIndex = ( (HierarchyDataRootPseudoScene) target.Root ).IndexOf( newParent ) + 1;

							newParent = null;
						}
						else
						{
							// Dropped above or below a regular Transform, calculate target sibling index
							if( insertDirection < 0 )
								newSiblingIndex = newParent.GetSiblingIndex();
							else
								newSiblingIndex = newParent.GetSiblingIndex() + 1;

							// To be able to drop the object at that sibling index, object's parent must also be changed
							newParent = newParent.parent;
						}
					}

					if( !newParent )
						newScene = target.Root;
				}

				int successfullyDroppedTransformCount = 0;
				for( int i = 0; i < droppedTransforms.Length; i++ )
				{
					bool _shouldFocusObjectInHierarchy, decrementSiblingIndex;
					if( DropTransformOnto( droppedTransforms[i], target, newScene, newParent, ( newSiblingIndex >= 0 ) ? ( newSiblingIndex + successfullyDroppedTransformCount ) : newSiblingIndex, out decrementSiblingIndex, out _shouldFocusObjectInHierarchy ) )
					{
						successfullyDroppedTransformCount++;
						shouldFocusObjectInHierarchy |= _shouldFocusObjectInHierarchy;

						if( decrementSiblingIndex )
							newSiblingIndex--;
					}
				}

				if( successfullyDroppedTransformCount == 0 )
					return;
			}

			// Don't reveal the selection unless it's necessary (i.e. selection is already fully visible)
			if( shouldFocusObjectInHierarchy )
				hierarchy.SelectInternal( droppedTransforms, RuntimeHierarchy.SelectOptions.FocusOnSelection | RuntimeHierarchy.SelectOptions.ForceRevealSelection );
			else
				hierarchy.Refresh();
		}

		private bool DropTransformOnto( Transform droppedTransform, HierarchyData target, HierarchyDataRoot newScene, Transform newParent, int newSiblingIndex, out bool decrementSiblingIndex, out bool shouldFocusObjectInHierarchy )
		{
			shouldFocusObjectInHierarchy = false;
			decrementSiblingIndex = false;

			// If we are only changing the sibling index of the dropped Transform and not its parent, then make sure
			// that the target sibling index won't be affected when the dropped Transform is shifted in the Hierarchy
			if( droppedTransform.parent == newParent || ( newScene != null && newScene.IndexOf( droppedTransform ) >= 0 ) )
			{
				if( newParent || ( newScene is HierarchyDataRootScene && ( (HierarchyDataRootScene) newScene ).Scene == droppedTransform.gameObject.scene ) )
				{
					if( newSiblingIndex > droppedTransform.GetSiblingIndex() )
					{
						newSiblingIndex--;
						decrementSiblingIndex = true;
					}
				}
				else if( newScene is HierarchyDataRootPseudoScene )
				{
					int pseudoSceneSiblingIndex = newScene.IndexOf( droppedTransform );
					if( pseudoSceneSiblingIndex >= 0 && newSiblingIndex > pseudoSceneSiblingIndex )
					{
						newSiblingIndex--;
						decrementSiblingIndex = true;
					}
				}
			}

			if( newParent ) // Drop inside a Transform
			{
				if( !hierarchy.CanDropDraggedParentOnChild )
				{
					// Avoid setting child object as parent of the parent object
					if( newParent.IsChildOf( droppedTransform ) )
						return false;
				}
				else
				{
					Transform curr = newParent;
					while( curr.parent != null && curr.parent != droppedTransform )
						curr = curr.parent;

					// First, set the child object's parent as the dropped object's current parent so that
					// the dropped object can then become a child of the former child object
					if( curr.parent == droppedTransform )
					{
						HierarchyDataRootPseudoScene pseudoScene = target.Root as HierarchyDataRootPseudoScene;
						int pseudoSceneChildIndex;
						if( pseudoScene != null && ( pseudoSceneChildIndex = pseudoScene.IndexOf( droppedTransform ) ) >= 0 )
						{
							// Dropped object was a root pseudo-scene object, swap the child and parent objects in the pseudo-scene, as well
							if( hierarchy.CanDropDraggedObjectsToPseudoScenes )
							{
								pseudoScene.InsertChild( pseudoSceneChildIndex, curr );
								pseudoScene.RemoveChild( droppedTransform );
							}
						}

						int siblingIndex = droppedTransform.GetSiblingIndex();
						curr.SetParent( droppedTransform.parent, true );
						curr.SetSiblingIndex( siblingIndex );

						shouldFocusObjectInHierarchy = true;
					}
				}

				droppedTransform.SetParent( newParent, true );
			}
			else // Drop at the root of a scene
			{
				if( newScene is HierarchyDataRootPseudoScene )
				{
					if( !hierarchy.CanDropDraggedObjectsToPseudoScenes )
						return false;

					// Add object to pseudo-scene
					if( newSiblingIndex < 0 )
						( (HierarchyDataRootPseudoScene) newScene ).AddChild( droppedTransform );
					else
					{
						( (HierarchyDataRootPseudoScene) newScene ).InsertChild( newSiblingIndex, droppedTransform );

						// Don't try to change the actual sibling index of the Transform
						newSiblingIndex = -1;
						target = newScene;
					}
				}
				else if( newScene is HierarchyDataRootScene )
				{
					if( droppedTransform.parent != null )
						droppedTransform.SetParent( null, true );

					// Change dropped object's scene
					Scene scene = ( (HierarchyDataRootScene) newScene ).Scene;
					if( droppedTransform.gameObject.scene != scene )
						SceneManager.MoveGameObjectToScene( droppedTransform.gameObject, scene );

					if( newSiblingIndex < 0 )
					{
						// If object was dropped onto the scene, add it to the bottom of the scene
						newSiblingIndex = scene.rootCount + 1;
						shouldFocusObjectInHierarchy = true;
					}
				}
			}

			if( newSiblingIndex >= 0 )
				droppedTransform.SetSiblingIndex( newSiblingIndex );

			shouldFocusObjectInHierarchy |= ( newSiblingIndex < 0 && !target.IsExpanded );
			return true;
		}

		// Returns -1 if t1 is above t2 in Hierarchy, 1 if t1 is below t2 in Hierarchy and 0 if they are the same object
		private int CompareHierarchySiblingIndices( Transform t1, Transform t2 )
		{
			Transform parent1 = t1.parent;
			Transform parent2 = t2.parent;

			if( parent1 == parent2 )
				return t1.GetSiblingIndex() - t2.GetSiblingIndex();

			int deltaHierarchyDepth = 0;
			for( ; parent1; parent1 = parent1.parent )
				deltaHierarchyDepth++;
			for( ; parent2; parent2 = parent2.parent )
				deltaHierarchyDepth--;

			for( ; deltaHierarchyDepth > 0; deltaHierarchyDepth-- )
			{
				t1 = t1.parent;
				if( t1 == t2 )
					return 1;
			}
			for( ; deltaHierarchyDepth < 0; deltaHierarchyDepth++ )
			{
				t2 = t2.parent;
				if( t1 == t2 )
					return -1;
			}

			while( t1.parent != t2.parent )
			{
				t1 = t1.parent;
				t2 = t2.parent;
			}

			return t1.GetSiblingIndex() - t2.GetSiblingIndex();
		}

		void IPointerEnterHandler.OnPointerEnter( PointerEventData eventData )
		{
			if( !hierarchy.CanReorganizeItems || hierarchy.IsInSearchMode )
				return;

			if( !RuntimeInspectorUtils.GetAssignableObjectFromDraggedReferenceItem<Transform>( eventData ) )
				return;

			pointer = eventData;
			pointerLastYPos = -1f;
			nextPointerValidation = POINTER_VALIDATE_INTERVAL;

			if( canvas.renderMode == RenderMode.ScreenSpaceOverlay || ( canvas.renderMode == RenderMode.ScreenSpaceCamera && !canvas.worldCamera ) )
				worldCamera = null;
			else
				worldCamera = canvas.worldCamera ? canvas.worldCamera : Camera.main;

			Update();
		}

		void IPointerExitHandler.OnPointerExit( PointerEventData eventData )
		{
			pointer = null;
			worldCamera = null;

			if( dragDropTargetVisualization.gameObject.activeSelf )
				dragDropTargetVisualization.gameObject.SetActive( false );

			hierarchy.AutoScrollSpeed = 0f;
		}
	}
}