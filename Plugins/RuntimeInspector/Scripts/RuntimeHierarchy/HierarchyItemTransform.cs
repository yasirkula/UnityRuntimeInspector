using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RuntimeInspectorNamespace
{
	public class HierarchyItemTransform : HierarchyItem
	{
		protected override bool IsValid { get { return !BoundTransform.IsNull(); } }
		protected override int ChildCount { get { return BoundTransform.childCount; } }

		public Transform BoundTransform { get; private set; }

		private IEnumerator pointerHeldCoroutine = null;
		private float nextNameRefreshTime = -1f;
		
		private bool m_isActive = true;
		protected override bool IsActive
		{
			get { return m_isActive; }
			set
			{
				if( m_isActive != value )
				{
					m_isActive = value;

					Color color = nameText.color;
					color.a = IsActive ? 1f : INACTIVE_ITEM_TEXT_ALPHA;
					nameText.color = color;
				}
			}
		}

		protected override void Initialize()
		{
			base.Initialize();

			clickListener.PointerDown += OnPointerDown;
			clickListener.PointerUp += OnPointerUp;
		}

		public void BindTo( Transform target )
		{
			BoundTransform = target;
			nameText.text = target.name;

			Refresh();
		}

		public override void Refresh()
		{
			base.Refresh();

			if( Time.realtimeSinceStartup >= nextNameRefreshTime )
			{
				nextNameRefreshTime = Time.realtimeSinceStartup + Hierarchy.ObjectNamesRefreshInterval * ( IsSelected ? 0.5f : 1f );
				nameText.text = BoundTransform.name;
			}
		}

		private void OnPointerDown( PointerEventData eventData )
		{
			if( pointerHeldCoroutine != null )
				return;

			if( !Hierarchy.CreateDraggedReferenceOnHold )
				return;

			if( BoundTransform.IsNull() )
				return;

			pointerHeldCoroutine = CreateReferenceItemCoroutine( eventData );
			StartCoroutine( pointerHeldCoroutine );
		}

		private void OnPointerUp( PointerEventData eventData )
		{
			if( pointerHeldCoroutine != null )
			{
				StopCoroutine( pointerHeldCoroutine );
				pointerHeldCoroutine = null;
			}
		}

		public override void Unbind()
		{
			base.Unbind();
			BoundTransform = null;
		}

		private IEnumerator CreateReferenceItemCoroutine( PointerEventData eventData )
		{
			yield return new WaitForSecondsRealtime( Hierarchy.DraggedReferenceHoldTime );

			if( !BoundTransform.IsNull() )
				RuntimeInspectorUtils.CreateDraggedReferenceItem( BoundTransform, eventData, Skin );
		}

		protected override Transform GetChild( int index )
		{
			return BoundTransform.GetChild( index );
		}
	}
}