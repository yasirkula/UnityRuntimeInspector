using System;
using UnityEngine;

namespace RuntimeInspectorNamespace
{
	public class StringField : InspectorField
	{
		public enum Mode { OnValueChange = 0, OnSubmit = 1 };

		[SerializeField]
		private BoundInputField input;

		private Mode m_setterMode = Mode.OnValueChange;
		public Mode SetterMode
		{
			get { return m_setterMode; }
			set { m_setterMode = value; }
		}

		public override void Initialize()
		{
			base.Initialize();

			input.Initialize();
			input.OnValueChanged += OnValueChanged;
			input.OnValueSubmitted += OnValueSubmitted;
			input.DefaultEmptyValue = string.Empty;
		}

		public override bool SupportsType( Type type )
		{
			return type == typeof( string );
		}

		private bool OnValueChanged( BoundInputField source, string input )
		{
			if( m_setterMode == Mode.OnValueChange )
				Value = input;

			return true;
		}

		private bool OnValueSubmitted( BoundInputField source, string input )
		{
			if( m_setterMode == Mode.OnSubmit )
				Value = input;

			return true;
		}

		protected override void OnSkinChanged()
		{
			base.OnSkinChanged();
			input.Skin = Skin;
		}

		public override void Refresh()
		{
			base.Refresh();

			if( Value == null )
				input.Text = string.Empty;
			else
				input.Text = (string) Value;
		}
	}
}