using System;
using UnityEngine;
using UnityEngine.UI;

namespace RuntimeInspectorNamespace
{
	public class StringField : InspectorField
	{
		[SerializeField]
		private BoundInputField input;

		public override void Initialize()
		{
			base.Initialize();

			input.Initialize();
			input.OnValueChanged += OnValueChanged;
			input.DefaultEmptyValue = string.Empty;
		}

		public override bool SupportsType( Type type )
		{
			return type == typeof( string );
		}

		private bool OnValueChanged( BoundInputField source, string input )
		{
			Value = input;
			return true;
		}

		protected override void OnSkinChanged()
		{
			base.OnSkinChanged();
			input.Skin = Skin;
		}

		public override void Refresh()
		{
			base.Refresh();

			if( Value == null )
				input.Text = string.Empty;
			else
				input.Text = (string) Value;
		}
	}
}