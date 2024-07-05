using System.Reflection;
using UnityEngine;

namespace RuntimeInspectorNamespace
{
	public class TransformField : ExpandableInspectorField<Transform>
	{
		protected override int Length { get { return 3; } } // localPosition, localEulerAngles, localScale

		private PropertyInfo positionProp, rotationProp, scaleProp;

		public override void Initialize()
		{
			base.Initialize();

			positionProp = typeof( Transform ).GetProperty( "localPosition" );
			rotationProp = typeof( Transform ).GetProperty( "localEulerAngles" );
			scaleProp = typeof( Transform ).GetProperty( "localScale" );
		}

		protected override void GenerateElements()
		{
			CreateDrawerForVariable( positionProp, "Position" );
			CreateDrawerForVariable( rotationProp, "Rotation" );
			CreateDrawerForVariable( scaleProp, "Scale" );
		}
	}
}
