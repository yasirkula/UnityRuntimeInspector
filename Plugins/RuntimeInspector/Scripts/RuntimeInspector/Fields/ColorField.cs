using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RuntimeInspectorNamespace
{
	public class ColorField : InspectorField
	{
#pragma warning disable 0649
		[SerializeField]
		private PointerEventListener inputColor;
		private Image colorImg;
#pragma warning restore 0649

		private bool isColor32;

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

		protected override void OnBound( MemberInfo variable )
		{
			base.OnBound( variable );
			isColor32 = BoundVariableType == typeof( Color32 );
		}

		private void ShowColorPicker( PointerEventData eventData )
		{
			if( isColor32 )
				ColorPicker.Instance.Show( OnColorChanged, (Color32) Value );
			else
				ColorPicker.Instance.Show( OnColorChanged, (Color) Value );
		}

		private void OnColorChanged( Color32 color )
		{
			colorImg.color = color;

			if( isColor32 )
				Value = color;
			else
				Value = (Color) color;
		}

		public override void Refresh()
		{
			base.Refresh();

			if( isColor32 )
				colorImg.color = (Color32) Value;
			else
				colorImg.color = (Color) Value;
		}
	}
}