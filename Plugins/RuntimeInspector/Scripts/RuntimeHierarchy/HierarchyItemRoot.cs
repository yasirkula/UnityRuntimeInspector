using System.Collections.Generic;
using UnityEngine;

namespace RuntimeInspectorNamespace
{
	public class HierarchyItemRoot : HierarchyItem
	{
		protected override bool IsValid { get { return Content.IsValid; } }
		protected override int ChildCount { get { return childObjects.Count; } }

		private List<GameObject> childObjects = new List<GameObject>( 16 );
		public IHierarchyRootContent Content { get; private set; }

		public void BindTo( IHierarchyRootContent target )
		{
			if( !target.IsValid )
			{
				Unbind();
				return;
			}

			Content = target;
			Content.Children = childObjects;

			nameText.text = Content.Name;

			Refresh();
		}

		public override void Unbind()
		{
			base.Unbind();

			Content = null;
			childObjects.Clear();
        }

		protected override void OnSkinChanged()
		{
			NORMAL_COLOR = Skin.BackgroundColor.Tint( 0.075f );
			base.OnSkinChanged();
		}

		protected override void RefreshContent()
		{
			if( Content.IsValid )
				Content.Refresh();
		}
		
		protected override Transform GetChild( int index )
		{
			return childObjects[index].transform;
		}
	}
}