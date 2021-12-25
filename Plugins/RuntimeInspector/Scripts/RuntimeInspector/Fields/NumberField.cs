using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace RuntimeInspectorNamespace
{
    public class NumberField : InspectorField<IConvertible>
	{
		private static readonly HashSet<Type> supportedTypes = new HashSet<Type>()
		{
			typeof( int ), typeof( uint ), typeof( long ), typeof( ulong ),
			typeof( byte ), typeof( sbyte ), typeof( short ), typeof( ushort ), typeof( char ),
			typeof( float ), typeof( double ), typeof( decimal )
		};

#pragma warning disable 0649
		[SerializeField]
		protected BoundInputField input;
#pragma warning restore 0649

		protected INumberHandler numberHandler;

		public override void Initialize()
		{
			base.Initialize();

			input.Initialize();
			input.OnValueChanged += OnValueChanged;
			input.OnValueSubmitted += OnValueSubmitted;
			input.DefaultEmptyValue = "0";
		}

		protected override void OnBound( MemberInfo variable )
		{
			base.OnBound( variable );

			if( m_boundVariableType == typeof( float ) || m_boundVariableType == typeof( double ) || m_boundVariableType == typeof( decimal ) )
				input.BackingField.contentType = InputField.ContentType.DecimalNumber;
			else
				input.BackingField.contentType = InputField.ContentType.IntegerNumber;

			numberHandler = NumberHandlers.Get( m_boundVariableType );
			input.Text = Value.ToString( RuntimeInspectorUtils.numberFormat );
		}

		protected virtual bool OnValueChanged( BoundInputField source, string input )
		{
			if( numberHandler.TryParse( input, out IConvertible value ) )
			{
				Value = value;
				return true;
			}
			return false;
		}

		private bool OnValueSubmitted( BoundInputField source, string input )
		{
			Inspector.RefreshDelayed();
			return OnValueChanged( source, input );
		}

		protected override void OnSkinChanged()
		{
			base.OnSkinChanged();
			input.Skin = Skin;

			Vector2 rightSideAnchorMin = new Vector2( Skin.LabelWidthPercentage, 0f );
			variableNameMask.rectTransform.anchorMin = rightSideAnchorMin;
			( (RectTransform) input.transform ).anchorMin = rightSideAnchorMin;
		}

		public override void Refresh()
		{
			var prevVal = Value;
			base.Refresh();

			if( !Value.Equals( prevVal ) )
				input.Text = Value.ToString( RuntimeInspectorUtils.numberFormat );
		}
	}
}
