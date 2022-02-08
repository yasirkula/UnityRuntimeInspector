using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RuntimeInspectorNamespace
{
	public class ColorField : InspectorField<Color>
	{
#pragma warning disable 0649
		[SerializeField]
		private RectTransform colorPickerArea;

		[SerializeField]
		private PointerEventListener inputColor;

		[SerializeField]
		private Text multiValueText;

		private Image colorImg;

#pragma warning restore 0649

		public override void Initialize()
		{
			base.Initialize();

			colorImg = inputColor.GetComponent<Image>();
			inputColor.PointerClick += ShowColorPicker;
		}

		public override bool SupportsType( Type type )
		{
			return type == typeof( Color ) || type == typeof( Color32 );
		}

		private void ShowColorPicker( PointerEventData eventData )
		{
			var initialBoundValues = BoundValues;
			Color? value = BoundValues.GetSingle();

			ColorPicker.Instance.Skin = Inspector.Skin;
			ColorPicker.Instance.Show(
				OnColorChanged,
				null,
				value.HasValue ? value.Value : Color.white,
				Inspector.Canvas,
				() => BoundValues = initialBoundValues );
		}

		private void OnColorChanged( Color32 color )
		{
			colorImg.color = color;
			BoundValues = new Color[] { color }.AsReadOnly();
		}

		protected override void OnSkinChanged()
		{
			base.OnSkinChanged();

			Vector2 rightSideAnchorMin = new Vector2( Skin.LabelWidthPercentage, 0f );
			variableNameMask.rectTransform.anchorMin = rightSideAnchorMin;
			colorPickerArea.anchorMin = rightSideAnchorMin;
			multiValueText.SetSkinInputFieldText( Skin );
		}

		public override void Refresh()
		{
			base.Refresh();
			Color? value = BoundValues.GetSingle();
			if( value.HasValue )
			{
				multiValueText.enabled = false;
				colorImg.color = value.Value;
			}
			else
			{
				multiValueText.enabled = true;
				colorImg.color = Skin.InputFieldNormalBackgroundColor;
			}
		}
	}
}
