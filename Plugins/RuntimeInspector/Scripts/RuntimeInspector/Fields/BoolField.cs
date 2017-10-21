using System;
using UnityEngine;
using UnityEngine.UI;

namespace RuntimeInspectorNamespace
{
	public class BoolField : InspectorField
	{
		[SerializeField]
		private Image toggleBackground;

		[SerializeField]
		private Toggle input;

		public override void Initialize()
		{
			base.Initialize();
			input.onValueChanged.AddListener( OnValueChanged );
		}

		public override bool SupportsType( Type type )
		{
			return type == typeof( bool );
		}

		private void OnValueChanged( bool input )
		{
			Value = input;
		}

		protected override void OnSkinChanged()
		{
			base.OnSkinChanged();

			toggleBackground.color = Skin.InputFieldNormalBackgroundColor;
			input.graphic.color = Skin.ToggleCheckmarkColor;
		}

		public override void Refresh()
		{
			base.Refresh();
			input.isOn = (bool) Value;
		}
	}
}