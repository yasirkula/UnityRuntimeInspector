using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace RuntimeInspectorNamespace
{
	public class NumberField : InspectorField
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

		public override bool SupportsType( Type type )
		{
			return supportedTypes.Contains( type );
		}

		protected override void OnBound( MemberInfo variable )
		{
			base.OnBound( variable );

			if( BoundVariableType == typeof( float ) || BoundVariableType == typeof( double ) || BoundVariableType == typeof( decimal ) )
				input.BackingField.contentType = InputField.ContentType.DecimalNumber;
			else
				input.BackingField.contentType = InputField.ContentType.IntegerNumber;

			numberHandler = NumberHandlers.Get( BoundVariableType );
			input.Text = numberHandler.ToString( Value );
		}

		protected virtual bool OnValueChanged( BoundInputField source, string input )
		{
			object value;
			if( numberHandler.TryParse( input, out value ) )
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
			object prevVal = Value;
			base.Refresh();

			if( !numberHandler.ValuesAreEqual( Value, prevVal ) )
				input.Text = numberHandler.ToString( Value );
		}
	}
}