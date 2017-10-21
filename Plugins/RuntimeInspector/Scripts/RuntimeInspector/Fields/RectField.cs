using System;
using UnityEngine;
using UnityEngine.UI;

namespace RuntimeInspectorNamespace
{
	public class RectField : InspectorField
	{
		[SerializeField]
		private BoundInputField inputX;

		[SerializeField]
		private BoundInputField inputY;

		[SerializeField]
		private BoundInputField inputW;

		[SerializeField]
		private BoundInputField inputH;

		[SerializeField]
		private Text labelX;

		[SerializeField]
		private Text labelY;

		[SerializeField]
		private Text labelW;

		[SerializeField]
		private Text labelH;

		protected override float HeightMultiplier { get { return 2f; } }

		public override void Initialize()
		{
			base.Initialize();

			inputX.Initialize();
			inputY.Initialize();
			inputW.Initialize();
			inputH.Initialize();

			inputX.OnValueChanged += OnValueChanged;
			inputY.OnValueChanged += OnValueChanged;
			inputW.OnValueChanged += OnValueChanged;
			inputH.OnValueChanged += OnValueChanged;

			inputX.DefaultEmptyValue = "0";
			inputY.DefaultEmptyValue = "0";
			inputW.DefaultEmptyValue = "0";
			inputH.DefaultEmptyValue = "0";
		}

		public override bool SupportsType( Type type )
		{
			return type == typeof( Rect );
		}

		protected override void OnBound()
		{
			base.OnBound();
			
			Rect val = (Rect) Value;
			inputX.Text = "" + val.x;
			inputY.Text = "" + val.y;
			inputW.Text = "" + val.width;
			inputH.Text = "" + val.height;
		}

		private bool OnValueChanged( BoundInputField source, string input )
		{
			float value;
			if( float.TryParse( input, out value ) )
			{
				Rect val = (Rect) Value;
				if( source == inputX )
					val.x = value;
				else if( source == inputY )
					val.y = value;
				else if( source == inputW )
					val.width = value;
				else
					val.height = value;

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
			labelW.SetSkinText( Skin );
			labelH.SetSkinText( Skin );

			inputX.Skin = Skin;
			inputY.Skin = Skin;
			inputW.Skin = Skin;
			inputH.Skin = Skin;
		}

		public override void Refresh()
		{
			Rect prevVal = (Rect) Value;
			base.Refresh();
			Rect val = (Rect) Value;

			if( val.x != prevVal.x )
				inputX.Text = "" + val.x;
			if( val.y != prevVal.y )
				inputY.Text = "" + val.y;
			if( val.width != prevVal.width )
				inputW.Text = "" + val.width;
			if( val.height != prevVal.height )
				inputH.Text = "" + val.height;
		}
	}
}