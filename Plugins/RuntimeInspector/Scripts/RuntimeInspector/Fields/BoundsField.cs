using System;
using System.Reflection;
using UnityEngine;

namespace RuntimeInspectorNamespace
{
	public class BoundsField : InspectorField
	{
#pragma warning disable 0649
		[SerializeField]
		private Vector3Field inputCenter;
		[SerializeField]
		private Vector3Field inputExtents;
#pragma warning restore 0649

		private MemberInfo centerVariable;
		private MemberInfo extentsVariable;

#if UNITY_2017_2_OR_NEWER
		private MemberInfo intCenterVariable;
		private MemberInfo intSizeVariable;
#endif

		protected override float HeightMultiplier { get { return 3f; } }

		public override void Initialize()
		{
			base.Initialize();

			centerVariable = typeof( Bounds ).GetProperty( "center" );
			extentsVariable = typeof( Bounds ).GetProperty( "extents" );
#if UNITY_2017_2_OR_NEWER
			intCenterVariable = typeof( BoundsInt ).GetProperty( "center" );
			intSizeVariable = typeof( BoundsInt ).GetProperty( "size" );
#endif
		}

		public override bool SupportsType( Type type )
		{
#if UNITY_2017_2_OR_NEWER
			if( type == typeof( BoundsInt ) )
				return true;
#endif
			return type == typeof( Bounds );
		}

		protected override void OnBound( MemberInfo variable )
		{
			base.OnBound( variable );

#if UNITY_2017_2_OR_NEWER
			if( BoundVariableType == typeof( BoundsInt ) )
			{
				inputCenter.BindTo( this, intCenterVariable, "Center:" );
				inputExtents.BindTo( this, intSizeVariable, "Size:" );
			}
			else
#endif
			{
				inputCenter.BindTo( this, centerVariable, "Center:" );
				inputExtents.BindTo( this, extentsVariable, "Extents:" );
			}
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