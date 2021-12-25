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
			ColorPicker.Instance.Skin = Inspector.Skin;
			ColorPicker.Instance.Show( OnColorChanged, null, Value, Inspector.Canvas );
		}

		private void OnColorChanged( Color32 color )
		{
			colorImg.color = color;
			Value = color;
		}

		protected override void OnSkinChanged()
		{
			base.OnSkinChanged();

			Vector2 rightSideAnchorMin = new Vector2( Skin.LabelWidthPercentage, 0f );
			variableNameMask.rectTransform.anchorMin = rightSideAnchorMin;
			colorPickerArea.anchorMin = rightSideAnchorMin;
		}

		public override void Refresh()
		{
			base.Refresh();
			colorImg.color = Value;
		}
	}
}
