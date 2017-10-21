using System;
using System.Reflection;
using UnityEngine;

namespace RuntimeInspectorNamespace
{
	public class BoundsField : InspectorField
	{
		[SerializeField]
		private Vector3Field inputCenter;
		[SerializeField]
		private Vector3Field inputExtents;

		private MemberInfo centerVariable;
		private MemberInfo extentsVariable;

		protected override float HeightMultiplier { get { return 3f; } }

		public override void Initialize()
		{
			base.Initialize();
			
			centerVariable = typeof( Bounds ).GetProperty( "center" );
			extentsVariable = typeof( Bounds ).GetProperty( "extents" );
		}

		public override bool SupportsType( Type type )
		{
			return type == typeof( Bounds );
		}

		protected override void OnBound()
		{
			base.OnBound();
			
			inputCenter.BindTo( this, centerVariable, "Center:" );
			inputExtents.BindTo( this, extentsVariable, "Extents:" );
		}

		protected override void OnInspectorChanged()
		{
			base.OnInspectorChanged();

			inputCenter.Inspector = Inspector;
			inputExtents.Inspector = Inspector;
		}

		protected override void OnSkinChanged()
		{
			base.OnSkinChanged();

			inputCenter.Skin = Skin;
			inputExtents.Skin = Skin;
		}

		protected override void OnDepthChanged()
		{
			base.OnDepthChanged();

			inputCenter.Depth = Depth + 1;
			inputExtents.Depth = Depth + 1;
		}
		
		public override void Refresh()
		{
			base.Refresh();

			inputCenter.Refresh();
			inputExtents.Refresh();
		}
	}
}