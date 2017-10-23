using System;
using UnityEngine;
using UnityEngine.UI;

namespace RuntimeInspectorNamespace
{
	public class DecimalField : InspectorField
	{
		private struct NumberParser
		{
			delegate bool ParseFunc( string input, out object value );
			delegate bool EqualsFunc( object value1, object value2 );

			private readonly ParseFunc parseFunction;
			private readonly EqualsFunc equalsFunction;

			public NumberParser( Type fieldType )
			{
				if( fieldType == typeof( float ) )
				{
					parseFunction = ( string input, out object value ) => { float parsedVal; bool result = float.TryParse( input, out parsedVal ); value = parsedVal; return result; };
					equalsFunction = ( object value1, object value2 ) => (float) value1 == (float) value2;
				}
				else if( fieldType == typeof( double ) )
				{
					parseFunction = ( string input, out object value ) => { double parsedVal; bool result = double.TryParse( input, out parsedVal ); value = parsedVal; return result; };
					equalsFunction = ( object value1, object value2 ) => (double) value1 == (double) value2;
				}
				else if( fieldType == typeof( decimal ) )
				{
					parseFunction = ( string input, out object value ) => { decimal parsedVal; bool result = decimal.TryParse( input, out parsedVal ); value = parsedVal; return result; };
					equalsFunction = ( object value1, object value2 ) => (decimal) value1 == (decimal) value2;
				}
				else
				{
					parseFunction = null;
					equalsFunction = null;
				}
            }

			public bool TryParse( string input, out object value )
			{
				return parseFunction( input, out value );
			}

			public bool ValuesAreEqual( object value1, object value2 )
			{
				return equalsFunction( value1, value2 );
			}
		}
		
		[SerializeField]
		private BoundInputField input;
		private NumberParser parser;

		public override void Initialize()
		{
			base.Initialize();

			input.Initialize();
			input.OnValueChanged += OnValueChanged;
			input.DefaultEmptyValue = "0";
		}

		public override bool SupportsType( Type type )
		{
			return type == typeof( float ) || type == typeof( double ) || type == typeof( decimal );
		}

		protected override void OnBound()
		{
			base.OnBound();

			parser = new NumberParser( BoundVariableType );
			input.Text = "" + Value;
		}

		private bool OnValueChanged( BoundInputField source, string input )
		{
			object value;
			if( parser.TryParse( input, out value ) )
			{
				Value = value;
				return true;
			}

			return false;
		}

		protected override void OnSkinChanged()
		{
			base.OnSkinChanged();
			input.Skin = Skin;
		}

		public override void Refresh()
		{
			object prevVal = Value;
			base.Refresh();

			if( !parser.ValuesAreEqual( Value, prevVal ) )
				input.Text = "" + Value;
		}
	}
}