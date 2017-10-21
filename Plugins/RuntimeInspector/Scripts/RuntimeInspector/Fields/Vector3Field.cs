using System;
using UnityEngine;
using UnityEngine.UI;

namespace RuntimeInspectorNamespace
{
	public class Vector3Field : InspectorField
	{
		[SerializeField]
		private BoundInputField inputX;

		[SerializeField]
		private BoundInputField inputY;

		[SerializeField]
		private BoundInputField inputZ;

		[SerializeField]
		private Text labelX;

		[SerializeField]
		private Text labelY;

		[SerializeField]
		private Text labelZ;

		public override void Initialize()
		{
			base.Initialize();

			inputX.Initialize();
			inputY.Initialize();
			inputZ.Initialize();
			
			inputX.OnValueChanged += OnValueChanged;
			inputY.OnValueChanged += OnValueChanged;
			inputZ.OnValueChanged += OnValueChanged;

			inputX.DefaultEmptyValue = "0";
			inputY.DefaultEmptyValue = "0";
			inputZ.DefaultEmptyValue = "0";
		}

		public override bool SupportsType( Type type )
		{
			return type == typeof( Vector3 );
		}

		protected override void OnBound()
		{
			base.OnBound();

			Vector3 val = (Vector3) Value;
			inputX.Text = "" + val.x;
			inputY.Text = "" + val.y;
			inputZ.Text = "" + val.z;
		}

		private bool OnValueChanged( BoundInputField source, string input )
		{
			float value;
			if( float.TryParse( input, out value ) )
			{
				Vector3 val = (Vector3) Value;
				if( source == inputX )
					val.x = value;
				else if( source == inputY )
					val.y = value;
				else
					val.z = value;

				Value = val;
				return true;
			}

			return false;
		}

		protected override void OnSkinChanged()
		{
			base.OnSkinChanged();

			labelX.SetSkinText( Skin );
			labelY.SetSkinText( Skin );
			labelZ.SetSkinText( Skin );

			inputX.Skin = Skin;
			inputY.Skin = Skin;
			inputZ.Skin = Skin;
		}

		public override void Refresh()
		{
			Vector3 prevVal = (Vector3) Value;
			base.Refresh();
			Vector3 val = (Vector3) Value;

			if( val.x != prevVal.x )
				inputX.Text = "" + val.x;
			if( val.y != prevVal.y )
				inputY.Text = "" + val.y;
			if( val.z != prevVal.z )
				inputZ.Text = "" + val.z;
		}
	}
}