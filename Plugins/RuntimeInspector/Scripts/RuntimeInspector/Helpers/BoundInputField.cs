using UnityEngine;
using UnityEngine.UI;

namespace RuntimeInspectorNamespace
{
	public class BoundInputField : MonoBehaviour
	{
		public delegate bool OnValueChangedDelegate( BoundInputField source, string input );

		private bool initialized = false;
		private bool inputValid = true;
		private bool inputAltered = false;

		private InputField inputField;
		private Image inputFieldBackground;
		public InputField BackingField { get { return inputField; } }

		[System.NonSerialized]
		public string DefaultEmptyValue = string.Empty;

		[System.NonSerialized]
		public bool CacheTextOnValueChange = true;

		private string recentText = string.Empty;
		public string Text
		{
			get { return inputField.text; }
			set
			{
				recentText = value;

				if( !inputField.isFocused )
				{
					inputValid = true;

					inputField.text = value;
					inputFieldBackground.color = Skin.InputFieldNormalBackgroundColor;
				}
			}
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
					Initialize();

					m_skin = value;
					m_skinVersion = m_skin.Version;

					inputField.textComponent.SetSkinInputFieldText( m_skin );
					inputFieldBackground.color = m_skin.InputFieldNormalBackgroundColor;

					Text placeholder = inputField.placeholder as Text;
					if( placeholder != null )
					{
						float placeholderAlpha = placeholder.color.a;
						placeholder.SetSkinInputFieldText( m_skin );

						Color placeholderColor = placeholder.color;
						placeholderColor.a = placeholderAlpha;
						placeholder.color = placeholderColor;
					}
				}
			}
		}

		public OnValueChangedDelegate OnValueChanged;
		public OnValueChangedDelegate OnValueSubmitted;

		private void Awake()
		{
			Initialize();
		}

		public void Initialize()
		{
			if( initialized )
				return;

			inputField = GetComponent<InputField>();
			inputFieldBackground = GetComponent<Image>();

			inputField.onValueChanged.AddListener( InputFieldValueChanged );
			inputField.onEndEdit.AddListener( InputFieldValueSubmitted );

			initialized = true;
		}

		private void InputFieldValueChanged( string str )
		{
			if( !inputField.isFocused )
				return;

			inputAltered = true;

			if( str == null || str.Length == 0 )
				str = DefaultEmptyValue;

			if( OnValueChanged != null )
			{
				inputValid = OnValueChanged( this, str );
				if( inputValid && CacheTextOnValueChange )
					recentText = str;

				inputFieldBackground.color = inputValid ? Skin.InputFieldNormalBackgroundColor : Skin.InputFieldInvalidBackgroundColor;
			}
		}

		private void InputFieldValueSubmitted( string str )
		{
			inputFieldBackground.color = Skin.InputFieldNormalBackgroundColor;

			if( !inputAltered )
			{
				inputField.text = recentText;
				return;
			}

			inputAltered = false;

			if( str == null || str.Length == 0 )
				str = DefaultEmptyValue;

			if( OnValueSubmitted != null )
			{
				if( OnValueSubmitted( this, str ) )
					recentText = str;
			}
			else if( inputValid )
				recentText = str;

			inputField.text = recentText;
			inputValid = true;
		}
	}
}