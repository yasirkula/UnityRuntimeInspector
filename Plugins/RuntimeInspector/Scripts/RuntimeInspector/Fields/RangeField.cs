using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace RuntimeInspectorNamespace
{
	public class RangeField : InspectorField
	{
		[SerializeField]
		private Image sliderBackground;

		[SerializeField]
		private Slider input;

        [SerializeField]
        private Text value;

        public override void Initialize()
		{
			base.Initialize();
			input.onValueChanged.AddListener( OnValueChanged );
		}

		public override bool SupportsType( Type type )
		{
            return type == typeof(RangeAttribute);
        }

        protected override void OnBound(MemberInfo member)
        {
            base.OnBound(member);

            RangeAttribute rangeAttribute = null;
            if (member is FieldInfo)
                rangeAttribute = ((FieldInfo)member).GetCustomAttribute<RangeAttribute>();
            else if (member is PropertyInfo)
                rangeAttribute = ((PropertyInfo)member).GetCustomAttribute<RangeAttribute>();

            if (rangeAttribute != null)
            {
                // Setting min/max will call OnValueChanged and replace our value with whatever the slider previously had (which
                // could be a recycled value), so store the correct value and restore after setting up min/max.
                var value = Value;
                input.minValue = rangeAttribute.min;
                input.maxValue = rangeAttribute.max;
                input.value = (float)value;
            }
        }


        private void OnValueChanged( float value )
		{
            if (Value.GetType() == typeof(int))
                Value = Mathf.RoundToInt(value);
            else
			    Value = value;
		}

		protected override void OnSkinChanged()
		{
			base.OnSkinChanged();

            sliderBackground.color = Skin.InputFieldNormalBackgroundColor;
            value.color = Skin.InputFieldTextColor;
            //nput.graphic.color = Skin.ToggleCheckmarkColor;
        }

		public override void Refresh()
		{
			base.Refresh();
            if (Value.GetType() == typeof(int))
            {
                input.value = (int)Value;
                value.text = ((int)Value).ToString();
            }
            else
            {
                input.value = (float)Value;
                value.text = ((float)Value).ToString("F3");
            }
        }
	}
}