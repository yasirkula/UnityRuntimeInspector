using UnityEngine;
using UnityEngine.UI;

namespace RuntimeInspectorNamespace
{
	public static class SkinUtils
	{
		public static void SetSkinText( this Text text, UISkin skin )
		{
			text.color = skin.TextColor;
			text.font = skin.Font;
			text.fontSize = skin.FontSize;
		}

		public static void SetSkinInputFieldText( this Text text, UISkin skin )
		{
			text.color = skin.InputFieldTextColor;
			text.font = skin.Font;
			text.fontSize = skin.FontSize;
		}

		public static void SetSkinButtonText( this Text text, UISkin skin )
		{
			text.color = skin.ButtonTextColor;
			text.font = skin.Font;
			text.fontSize = skin.FontSize;
		}

		public static void SetSkinButton( this Button button, UISkin skin )
		{
			button.targetGraphic.color = skin.ButtonBackgroundColor;
			button.GetComponentInChildren<Text>().SetSkinButtonText( skin );
		}

		public static void SetWidth( this LayoutElement layoutElement, float width )
		{
			layoutElement.minWidth = width;
			layoutElement.preferredWidth = width;
		}

		public static void SetHeight( this LayoutElement layoutElement, float height )
		{
			layoutElement.minHeight = height;
			layoutElement.preferredHeight = height;
		}

		public static void SetAnchorMinMaxInputField( this RectTransform inputField, RectTransform label, Vector2 anchorMin, Vector2 anchorMax )
		{
			inputField.anchorMin = anchorMin;
			inputField.anchorMax = anchorMax;
			label.anchorMin = anchorMin;
			label.anchorMax = new Vector2( anchorMin.x, anchorMax.y );
		}
	}
}