using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace RuntimeInspectorNamespace
{
	public class NumberRangeField : NumberField
	{
#pragma warning disable 0649
		[SerializeField]
		private BoundSlider slider;
#pragma warning restore 0649

		public override void Initialize()
		{
			base.Initialize();
			slider.OnValueChanged += OnSliderValueChanged;
		}

		public override bool CanBindTo( Type type, MemberInfo variable )
		{
			return variable != null && variable.HasAttribute<RangeAttribute>();
		}

		protected override void OnBound( MemberInfo variable )
		{
			base.OnBound( variable );

			RangeAttribute rangeAttribute = variable.GetAttribute<RangeAttribute>();
			slider.SetRange( Mathf.Max( rangeAttribute.min, numberHandler.MinValue ), Mathf.Min( rangeAttribute.max, numberHandler.MaxValue ) );
			slider.BackingField.wholeNumbers = BoundVariableType != typeof( float ) && BoundVariableType != typeof( double ) && BoundVariableType != typeof( decimal );
		}

		protected override bool OnValueChanged( BoundInputField source, string input )
		{
			object value;
			if( numberHandler.TryParse( input, out value ) )
			{
				float fvalue = numberHandler.ConvertToFloat( value );
				if( fvalue >= slider.BackingField.minValue && fvalue <= slider.BackingField.maxValue )
				{
					Value = value;
					return true;
				}
			}

			return false;
		}

		private void OnSliderValueChanged( BoundSlider source, float value )
		{
			if( input.BackingField.isFocused )
				return;

			Value = numberHandler.ConvertFromFloat( value );
			input.Text = numberHandler.ToString( Value );
			Inspector.RefreshDelayed();
		}

		protected override void OnSkinChanged()
		{
			base.OnSkinChanged();
			slider.Skin = Skin;

			float inputFieldWidth = ( 1f - Skin.LabelWidthPercentage ) / 3f;
			Vector2 rightSideAnchorMin = new Vector2( Skin.LabelWidthPercentage, 0f );
			variableNameMask.rectTransform.anchorMin = rightSideAnchorMin;
			( (RectTransform) slider.transform ).anchorMin = rightSideAnchorMin;
			( (RectTransform) slider.transform ).anchorMax = new Vector2( 1f - inputFieldWidth, 1f );
			( (RectTransform) input.transform ).anchorMin = new Vector2( 1f - inputFieldWidth, 0f );
		}

		public override void Refresh()
		{
			base.Refresh();
			slider.Value = numberHandler.ConvertToFloat( Value );
		}
	}
}