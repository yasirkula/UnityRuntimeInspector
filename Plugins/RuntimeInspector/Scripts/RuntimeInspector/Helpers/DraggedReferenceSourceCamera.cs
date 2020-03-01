using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RuntimeInspectorNamespace
{
	[RequireComponent( typeof( Camera ) )]
	public class DraggedReferenceSourceCamera : MonoBehaviour
	{
		public delegate Object RaycastHitProcesserDelegate( RaycastHit hit );

		private Camera _camera;

#pragma warning disable 0649
		[SerializeField]
		private UISkin draggedReferenceSkin;

		[SerializeField]
		private Canvas draggedReferenceCanvas;

		[SerializeField]
		private float holdTime = 0.4f;

		[SerializeField]
		private LayerMask interactableObjectsMask = ~0;

		[SerializeField]
		private float raycastRange = float.MaxValue;
#pragma warning restore 0649

		private bool pointerDown = false;
		private float pointerDownTime;
		private Vector2 pointerDownPos;

		private Object hitObject;

		private DraggedReferenceItem draggedReference;
		private PointerEventData draggingPointer;

		public RaycastHitProcesserDelegate ProcessRaycastHit;

		private void Awake()
		{
			_camera = GetComponent<Camera>();
		}

		private void Update()
		{
			if( draggingPointer != null )
			{
				if( !draggedReference || !draggedReference.gameObject.activeSelf )
					draggingPointer = null;
				else if( Input.GetMouseButtonUp( 0 ) )
				{
					ExecuteEvents.Execute( draggedReference.gameObject, draggingPointer, ExecuteEvents.endDragHandler );
					if( EventSystem.current != null )
					{
						List<RaycastResult> hoveredUIElements = new List<RaycastResult>();
						EventSystem.current.RaycastAll( draggingPointer, hoveredUIElements );

						int i = 0;
						while( i < hoveredUIElements.Count && ExecuteEvents.ExecuteHierarchy( hoveredUIElements[i].gameObject, draggingPointer, ExecuteEvents.dropHandler ) == null )
							i++;
					}

					draggingPointer = null;
				}
				else
				{
					draggingPointer.position = Input.mousePosition;
					ExecuteEvents.Execute( draggedReference.gameObject, draggingPointer, ExecuteEvents.dragHandler );
				}
			}
			else
			{
				if( !pointerDown )
				{
					if( Input.GetMouseButtonDown( 0 ) && EventSystem.current && !EventSystem.current.IsPointerOverGameObject() )
					{
						RaycastHit hit;
						if( Physics.Raycast( _camera.ScreenPointToRay( Input.mousePosition ), out hit, raycastRange, interactableObjectsMask ) )
						{
							hitObject = ( ProcessRaycastHit != null ) ? ProcessRaycastHit( hit ) : hit.collider.gameObject;
							if( hitObject )
							{
								pointerDown = true;
								pointerDownTime = Time.realtimeSinceStartup;
								pointerDownPos = Input.mousePosition;
							}
						}
					}
				}
				else
				{
					if( Input.GetMouseButton( 0 ) )
					{
						if( ( (Vector2) Input.mousePosition - pointerDownPos ).sqrMagnitude >= 100f )
							pointerDown = false;
						else if( Time.realtimeSinceStartup - pointerDownTime >= holdTime )
						{
							pointerDown = false;

							if( hitObject && EventSystem.current != null )
							{
								draggingPointer = new PointerEventData( EventSystem.current )
								{
									pointerId = Input.touchCount > 0 ? Input.GetTouch( 0 ).fingerId : -1,
									pressPosition = Input.mousePosition,
									position = Input.mousePosition,
									button = PointerEventData.InputButton.Left
								};

								draggedReference = RuntimeInspectorUtils.CreateDraggedReferenceItem( hitObject, draggingPointer, draggedReferenceSkin, draggedReferenceCanvas );
								if( draggedReference == null )
								{
									pointerDown = false;
									draggingPointer = null;
								}
							}
						}
					}
					else if( Input.GetMouseButtonUp( 0 ) )
						pointerDown = false;
				}
			}
		}
	}
}