using UnityEngine;
using UnityEngine.UI;

namespace RuntimeInspectorNamespace
{
	// Credit: http://answers.unity.com/answers/1157876/view.html
	[RequireComponent( typeof( CanvasRenderer ) )]
	public class NonDrawingMaskableGraphic : MaskableGraphic
	{
		public override void SetMaterialDirty() { return; }
		public override void SetVerticesDirty() { return; }

		protected override void OnPopulateMesh( VertexHelper vh )
		{
			vh.Clear();
			return;
		}
	}
}