using System;
using UnityEngine;
using UnityEngine.UI;

namespace RuntimeInspectorNamespace
{
	public class Vector4Field : InspectorField
	{
		[SerializeField]
		private BoundInputField inputX;

		[SerializeField]
		private BoundInputField inputY;

		[SerializeField]
		private BoundInputField inputZ;

		[SerializeField]
		private BoundInputField inputW;

		[SerializeField]
		private Text labelX;

		[SerializeField]
		private Text labelY;

		[SerializeField]
		private Text labelZ;

		[SerializeField]
		private Text labelW;

		private bool isQuaternion;

		protected override float HeightMultiplier { get { return 2f; } }

		public override void Initialize()
		{
			base.Initialize();

			inputX.Initialize();
			inputY.Initialize();
			inputZ.Initialize();
			inputW.Initialize();
			
			inputX.OnValueChanged += OnValueChanged;
			inputY.OnValueChanged += OnValueChanged;
			inputZ.OnValueChanged += OnValueChanged;
			inputW.OnValueChanged += OnValueChanged;

			inputX.DefaultEmptyValue = "0";
			inputY.DefaultEmptyValue = "0";
			inputZ.DefaultEmptyValue = "0";
			inputW.DefaultEmptyValue = "0";
		}

		public override bool SupportsType( Type type )
		{
			return type == typeof( Vector4 ) || type == typeof( Quaternion );
		}

		protected override void OnBound()
		{
			base.OnBound();

			isQuaternion = BoundVariableType == typeof( Quaternion );
			if( isQuaternion )
			{
				Quaternion val = (Quaternion) Value;
				inputX.Text = "" + val.x;
				inputY.Text = "" + val.y;
				inputZ.Text = "" + val.z;
				inputW.Text = "" + val.z;
			}
			else
			{
				Vector4 val = (Vector4) Value;
				inputX.Text = "" + val.x;
				inputY.Text = "" + val.y;
				inputZ.Text = "" + val.z;
				inputW.Text = "" + val.z;
			}
		}

		private bool OnValueChanged( BoundInputField source, string input )
		{
			float value;
			if( float.TryParse( input, out value ) )
			{
				if( isQuaternion )
				{
					Quaternion val = (Quaternion) Value;
					if( source == inputX )
						val.x = value;
					else if( source == inputY )
						val.y = value;
					else if( source == inputZ )
						val.z = value;
					else
						val.w = value;

					Value = val;
				}
				else
				{
					Vector4 val = (Vector4) Value;
					if( source == inputX )
						val.x = value;
					else if( source == inputY )
						val.y = value;
					else if( source == inputZ )
						val.z = value;
					else
						val.w = value;

					Value = val;
				}

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
			labelW.SetSkinText( Skin );

			inputX.Skin = Skin;
			inputY.Skin = Skin;
			inputZ.Skin = Skin;
			inputW.Skin = Skin;
		}

		public override void Refresh()
		{
			if( isQuaternion )
			{
				Quaternion prevVal = (Quaternion) Value;
				base.Refresh();
				Quaternion val = (Quaternion) Value;

				if( val.x != prevVal.x )
					inputX.Text = "" + val.x;
				if( val.y != prevVal.y )
					inputY.Text = "" + val.y;
				if( val.z != prevVal.z )
					inputZ.Text = "" + val.z;
				if( val.w != prevVal.w )
					inputW.Text = "" + val.z;
			}
			else
			{
				Vector4 prevVal = (Vector4) Value;
				base.Refresh();
				Vector4 val = (Vector4) Value;

				if( val.x != prevVal.x )
					inputX.Text = "" + val.x;
				if( val.y != prevVal.y )
					inputY.Text = "" + val.y;
				if( val.z != prevVal.z )
					inputZ.Text = "" + val.z;
				if( val.w != prevVal.w )
					inputW.Text = "" + val.z;
			}
		}
	}
}