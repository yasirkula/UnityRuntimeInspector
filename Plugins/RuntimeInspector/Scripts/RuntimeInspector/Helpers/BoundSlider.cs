using UnityEngine;
using UnityEngine.UI;

namespace RuntimeInspectorNamespace
{
	public class BoundSlider : MonoBehaviour
	{
		public delegate void OnValueChangedDelegate( BoundSlider source, float value );

#pragma warning disable 0649
		[SerializeField]
		private Slider slider;
		public Slider BackingField { get { return slider; } }

		[SerializeField]
		private Image sliderBackground;

		[SerializeField]
		private Image thumb;
#pragma warning restore 0649

		private bool sliderFocused = false;
		public bool IsFocused { get { return sliderFocused; } }

		public float Value
		{
			get { return slider.value; }
			set { slider.value = value; }
		}

		private int m_skinVersion = 0;
		private UISkin m_skin;
		public UISkin Skin
		{
			get { return m_skin; }
			set
			{
				if( m_skin != value || m_skinVersion != m_skin.Version )
				{
					m_skin = value;
					m_skinVersion = m_skin.Version;

					sliderBackground.color = m_skin.SliderBackgroundColor;
					thumb.color = m_skin.SliderThumbColor;
				}
			}
		}

		public OnValueChangedDelegate OnValueChanged;

		private void Awake()
		{
			PointerEventListener sliderPointerListener = slider.gameObject.AddComponent<PointerEventListener>();
			sliderPointerListener.PointerDown += ( ev ) => sliderFocused = true;
			sliderPointerListener.PointerUp += ( ev ) => sliderFocused = false;

			slider.onValueChanged.AddListener( SliderValueChanged );
		}

		private void OnDisable()
		{
			sliderFocused = false;
		}

		public void SetRange( float min, float max )
		{
			sliderFocused = false;

			if( min > max )
			{
				float temp = min;
				min = max;
				max = temp;
			}

			slider.minValue = min;
			slider.maxValue = max;
		}

		private void SliderValueChanged( float value )
		{
			if( !sliderFocused )
				return;

			if( OnValueChanged != null )
				OnValueChanged( this, value );
		}
	}
}