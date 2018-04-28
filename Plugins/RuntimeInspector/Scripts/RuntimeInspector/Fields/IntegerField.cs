#if !UNITY_EDITOR && NETFX_CORE
using System.Reflection;
#endif
using System;
using UnityEngine;

namespace RuntimeInspectorNamespace
{
	public class IntegerField : InspectorField
	{
		private struct NumberParser
		{
			delegate bool ParseFunc( string input, out object value );
			delegate bool EqualsFunc( object value1, object value2 );

			private readonly ParseFunc parseFunction;
			private readonly EqualsFunc equalsFunction;

			public NumberParser( Type fieldType )
			{
				if( fieldType == typeof( int ) )
				{
					parseFunction = ( string input, out object value ) => { int parsedVal; bool result = int.TryParse( input, out parsedVal ); value = parsedVal; return result; };
					equalsFunction = ( object value1, object value2 ) => (int) value1 == (int) value2;
				}
				else if( fieldType == typeof( uint ) )
				{
					parseFunction = ( string input, out object value ) => { uint parsedVal; bool result = uint.TryParse( input, out parsedVal ); value = parsedVal; return result; };
					equalsFunction = ( object value1, object value2 ) => (uint) value1 == (uint) value2;
				}
				else if( fieldType == typeof( long ) )
				{
					parseFunction = ( string input, out object value ) => { long parsedVal; bool result = long.TryParse( input, out parsedVal ); value = parsedVal; return result; };
					equalsFunction = ( object value1, object value2 ) => (long) value1 == (long) value2;
				}
				else if( fieldType == typeof( ulong ) )
				{
					parseFunction = ( string input, out object value ) => { ulong parsedVal; bool result = ulong.TryParse( input, out parsedVal ); value = parsedVal; return result; };
					equalsFunction = ( object value1, object value2 ) => (ulong) value1 == (ulong) value2;
				}
				else if( fieldType == typeof( byte ) )
				{
					parseFunction = ( string input, out object value ) => { byte parsedVal; bool result = byte.TryParse( input, out parsedVal ); value = parsedVal; return result; };
					equalsFunction = ( object value1, object value2 ) => (byte) value1 == (byte) value2;
				}
				else if( fieldType == typeof( sbyte ) )
				{
					parseFunction = ( string input, out object value ) => { sbyte parsedVal; bool result = sbyte.TryParse( input, out parsedVal ); value = parsedVal; return result; };
					equalsFunction = ( object value1, object value2 ) => (sbyte) value1 == (sbyte) value2;
				}
				else if( fieldType == typeof( short ) )
				{
					parseFunction = ( string input, out object value ) => { short parsedVal; bool result = short.TryParse( input, out parsedVal ); value = parsedVal; return result; };
					equalsFunction = ( object value1, object value2 ) => (short) value1 == (short) value2;
				}
				else if( fieldType == typeof( ushort ) )
				{
					parseFunction = ( string input, out object value ) => { ushort parsedVal; bool result = ushort.TryParse( input, out parsedVal ); value = parsedVal; return result; };
					equalsFunction = ( object value1, object value2 ) => (ushort) value1 == (ushort) value2;
				}
				else if( fieldType == typeof( char ) )
				{
					parseFunction = ( string input, out object value ) => { char parsedVal; bool result = char.TryParse( input, out parsedVal ); value = parsedVal; return result; };
					equalsFunction = ( object value1, object value2 ) => (char) value1 == (char) value2;
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
#if UNITY_EDITOR || !NETFX_CORE
			return type.IsPrimitive &&
#else
			return type.GetTypeInfo().IsPrimitive &&
#endif
				( type == typeof( int ) || type == typeof( uint ) ||
				type == typeof( long ) || type == typeof( ulong ) || type == typeof( byte ) || type == typeof( sbyte ) ||
				type == typeof( short ) || type == typeof( ushort ) || type == typeof( char ) );
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