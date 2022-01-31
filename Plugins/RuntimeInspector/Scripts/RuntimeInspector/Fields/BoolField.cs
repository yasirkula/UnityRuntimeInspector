using UnityEngine;
using UnityEngine.UI;

namespace RuntimeInspectorNamespace
{
	public class BoolField : InspectorField<bool>
	{
#pragma warning disable 0649
		[SerializeField]
		private Image toggleBackground;

		[SerializeField]
		private Toggle input;

		[SerializeField]
		private Image multiValueImage;
#pragma warning restore 0649

		public override void Initialize()
		{
			base.Initialize();
			input.onValueChanged.AddListener( OnValueChanged );
		}

		private void OnValueChanged( bool input )
		{
			BoundValues = new bool[] { input };
			Inspector.RefreshDelayed();
		}

		protected override void OnSkinChanged()
		{
			base.OnSkinChanged();

			toggleBackground.color = Skin.InputFieldNormalBackgroundColor;
			input.graphic.color = Skin.ToggleCheckmarkColor;
			multiValueImage.color = Skin.ToggleCheckmarkColor;

			Vector2 rightSideAnchorMin = new Vector2( Skin.LabelWidthPercentage, 0f );
			variableNameMask.rectTransform.anchorMin = rightSideAnchorMin;
			( (RectTransform) input.transform ).anchorMin = rightSideAnchorMin;
		}

		private void SwitchMarks( bool hasMultipleValues )
		{
			input.graphic.enabled = !hasMultipleValues;
			multiValueImage.enabled = hasMultipleValues;
		}

		public override void Refresh()
		{
			base.Refresh();
			bool? value = BoundValues.GetSingle();
			if( value.HasValue )
			{
				input.isOn = value.Value;
				SwitchMarks( false );
			}
			else
			{
				SwitchMarks( true );
			}
		}
	}
}
