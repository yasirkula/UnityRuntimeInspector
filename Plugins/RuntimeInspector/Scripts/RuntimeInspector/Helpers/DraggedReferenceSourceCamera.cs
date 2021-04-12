using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

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

#if !ENABLE_INPUT_SYSTEM || ENABLE_LEGACY_INPUT_MANAGER
		private readonly List<RaycastResult> hoveredUIElements = new List<RaycastResult>( 4 );
#endif

		public RaycastHitProcesserDelegate ProcessRaycastHit;

		private void Awake()
		{
			_camera = GetComponent<Camera>();
		}

		private void Update()
		{
#if !ENABLE_INPUT_SYSTEM || ENABLE_LEGACY_INPUT_MANAGER
			// On new Input System, DraggedReferenceItem's PointerEventData is tracked by DraggedReferenceItem itself, not this component
			if( draggingPointer != null )
			{
				if( !draggedReference || !draggedReference.gameObject.activeSelf )
					draggingPointer = null;
				else if( IsPointerHeld() )
				{
					draggingPointer.position = GetPointerPosition();
					ExecuteEvents.Execute( draggedReference.gameObject, draggingPointer, ExecuteEvents.dragHandler );
				}
				else
				{
					ExecuteEvents.Execute( draggedReference.gameObject, draggingPointer, ExecuteEvents.endDragHandler );
					if( EventSystem.current != null )
					{
						hoveredUIElements.Clear();
						EventSystem.current.RaycastAll( draggingPointer, hoveredUIElements );

						int i = 0;
						while( i < hoveredUIElements.Count && !ExecuteEvents.ExecuteHierarchy( hoveredUIElements[i].gameObject, draggingPointer, ExecuteEvents.dropHandler ) )
							i++;
					}

					draggingPointer = null;
				}
			}
			else
#endif
			{
				if( !pointerDown )
				{
					if( IsPointerDown() && EventSystem.current && !EventSystem.current.IsPointerOverGameObject() )
					{
						RaycastHit hit;
						if( Physics.Raycast( _camera.ScreenPointToRay( GetPointerPosition() ), out hit, raycastRange, interactableObjectsMask ) )
						{
							hitObject = ( ProcessRaycastHit != null ) ? ProcessRaycastHit( hit ) : hit.collider.gameObject;
							if( hitObject )
							{
								pointerDown = true;
								pointerDownTime = Time.realtimeSinceStartup;
								pointerDownPos = GetPointerPosition();
							}
						}
					}
				}
				else
				{
					if( IsPointerHeld() )
					{
						if( ( GetPointerPosition() - pointerDownPos ).sqrMagnitude >= 100f )
							pointerDown = false;
						else if( Time.realtimeSinceStartup - pointerDownTime >= holdTime )
						{
							pointerDown = false;

							if( hitObject && EventSystem.current )
							{
								draggingPointer = new PointerEventData( EventSystem.current )
								{
#if !ENABLE_INPUT_SYSTEM || ENABLE_LEGACY_INPUT_MANAGER
									pointerId = Input.touchCount > 0 ? Input.GetTouch( 0 ).fingerId : -1,
#endif
									pressPosition = GetPointerPosition(),
									position = GetPointerPosition(),
									button = PointerEventData.InputButton.Left
								};

								draggedReference = RuntimeInspectorUtils.CreateDraggedReferenceItem( hitObject, draggingPointer, draggedReferenceSkin, draggedReferenceCanvas );
								if( !draggedReference )
								{
									pointerDown = false;
									draggingPointer = null;
								}
							}
						}
					}
					else
						pointerDown = false;
				}
			}
		}

		private bool IsPointerDown()
		{
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
			return Pointer.current != null && Pointer.current.press.wasPressedThisFrame;
#else
			return Input.GetMouseButtonDown( 0 );
#endif
		}

		private bool IsPointerHeld()
		{
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
			return Pointer.current != null && Pointer.current.press.isPressed;
#else
			return Input.GetMouseButton( 0 );
#endif
		}

		private Vector2 GetPointerPosition()
		{
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
			return Pointer.current != null ? Pointer.current.position.ReadValue() : Vector2.zero;
#else
			return Input.mousePosition;
#endif
		}
	}
}