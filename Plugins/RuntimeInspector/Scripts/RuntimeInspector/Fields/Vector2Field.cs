using System;
using UnityEngine;
using UnityEngine.UI;

namespace RuntimeInspectorNamespace
{
	public class Vector2Field : InspectorField
	{
		[SerializeField]
		private BoundInputField inputX;

		[SerializeField]
		private BoundInputField inputY;

		[SerializeField]
		private Text labelX;

		[SerializeField]
		private Text labelY;

		public override void Initialize()
		{
			base.Initialize();

			inputX.Initialize();
			inputY.Initialize();

			inputX.OnValueChanged += OnValueChanged;
			inputY.OnValueChanged += OnValueChanged;

			inputX.DefaultEmptyValue = "0";
			inputY.DefaultEmptyValue = "0";
		}

		public override bool SupportsType( Type type )
		{
			return type == typeof( Vector2 );
		}

		protected override void OnBound()
		{
			base.OnBound();

			Vector2 val = (Vector2) Value;
			inputX.Text = "" + val.x;
			inputY.Text = "" + val.y;
		}

		private bool OnValueChanged( BoundInputField source, string input )
		{
			float value;
			if( float.TryParse( input, out value ) )
			{
				Vector2 val = (Vector2) Value;
				if( source == inputX )
					val.x = value;
				else
					val.y = value;

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

			inputX.Skin = Skin;
			inputY.Skin = Skin;
		}

		public override void Refresh()
		{
			Vector2 prevVal = (Vector2) Value;
			base.Refresh();
			Vector2 val = (Vector2) Value;

			if( val.x != prevVal.x )
				inputX.Text = "" + val.x;
			if( val.y != prevVal.y )
				inputY.Text = "" + val.y;
		}
	}
}