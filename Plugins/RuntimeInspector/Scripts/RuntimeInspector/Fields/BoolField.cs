using System;
using UnityEngine;
using UnityEngine.UI;

namespace RuntimeInspectorNamespace
{
	public class BoolField : InspectorField
	{
#pragma warning disable 0649
		[SerializeField]
		private Image toggleBackground;

		[SerializeField]
		private Toggle input;
#pragma warning restore 0649

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
			Inspector.RefreshDelayed();
		}

		protected override void OnSkinChanged()
		{
			base.OnSkinChanged();

			toggleBackground.color = Skin.InputFieldNormalBackgroundColor;
			input.graphic.color = Skin.ToggleCheckmarkColor;

			Vector2 rightSideAnchorMin = new Vector2( Skin.LabelWidthPercentage, 0f );
			variableNameMask.rectTransform.anchorMin = rightSideAnchorMin;
			( (RectTransform) input.transform ).anchorMin = rightSideAnchorMin;
		}

		public override void Refresh()
		{
			base.Refresh();
			input.isOn = (bool) Value;
		}
	}
}