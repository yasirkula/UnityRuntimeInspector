using System;
using System.Collections.Generic;
using System.Globalization;

namespace RuntimeInspectorNamespace
{
	public interface INumberHandler
	{
		float MinValue { get; }
		float MaxValue { get; }

		bool TryParse( string input, out object value );
		bool ValuesAreEqual( object value1, object value2 );

		object ConvertFromFloat( float value );
		float ConvertToFloat( object value );

		string ToString( object value );
	}

	public class NumberHandlers
	{
		#region Implementations
		private class IntHandler : INumberHandler
		{
			public float MinValue { get { return int.MinValue; } }
			public float MaxValue { get { return int.MaxValue; } }

			public bool TryParse( string input, out object value ) { int parsedVal; bool result = int.TryParse( input, NumberStyles.Integer, RuntimeInspectorUtils.numberFormat, out parsedVal ); value = parsedVal; return result; }
			public bool ValuesAreEqual( object value1, object value2 ) { return (int) value1 == (int) value2; }

			public object ConvertFromFloat( float value ) { return (int) value; }
			public float ConvertToFloat( object value ) { return (int) value; }

			public string ToString( object value ) { return ( (int) value ).ToString( RuntimeInspectorUtils.numberFormat ); }
		}

		private class UIntHandler : INumberHandler
		{
			public float MinValue { get { return uint.MinValue; } }
			public float MaxValue { get { return uint.MaxValue; } }

			public bool TryParse( string input, out object value ) { uint parsedVal; bool result = uint.TryParse( input, NumberStyles.Integer, RuntimeInspectorUtils.numberFormat, out parsedVal ); value = parsedVal; return result; }
			public bool ValuesAreEqual( object value1, object value2 ) { return (uint) value1 == (uint) value2; }

			public object ConvertFromFloat( float value ) { return (uint) value; }
			public float ConvertToFloat( object value ) { return (uint) value; }

			public string ToString( object value ) { return ( (uint) value ).ToString( RuntimeInspectorUtils.numberFormat ); }
		}

		private class LongHandler : INumberHandler
		{
			public float MinValue { get { return long.MinValue; } }
			public float MaxValue { get { return long.MaxValue; } }

			public bool TryParse( string input, out object value ) { long parsedVal; bool result = long.TryParse( input, NumberStyles.Integer, RuntimeInspectorUtils.numberFormat, out parsedVal ); value = parsedVal; return result; }
			public bool ValuesAreEqual( object value1, object value2 ) { return (long) value1 == (long) value2; }

			public object ConvertFromFloat( float value ) { return (long) value; }
			public float ConvertToFloat( object value ) { return (long) value; }

			public string ToString( object value ) { return ( (long) value ).ToString( RuntimeInspectorUtils.numberFormat ); }
		}

		private class ULongHandler : INumberHandler
		{
			public float MinValue { get { return ulong.MinValue; } }
			public float MaxValue { get { return ulong.MaxValue; } }

			public bool TryParse( string input, out object value ) { ulong parsedVal; bool result = ulong.TryParse( input, NumberStyles.Integer, RuntimeInspectorUtils.numberFormat, out parsedVal ); value = parsedVal; return result; }
			public bool ValuesAreEqual( object value1, object value2 ) { return (ulong) value1 == (ulong) value2; }

			public object ConvertFromFloat( float value ) { return (ulong) value; }
			public float ConvertToFloat( object value ) { return (ulong) value; }

			public string ToString( object value ) { return ( (ulong) value ).ToString( RuntimeInspectorUtils.numberFormat ); }
		}

		private class ByteHandler : INumberHandler
		{
			public float MinValue { get { return byte.MinValue; } }
			public float MaxValue { get { return byte.MaxValue; } }

			public bool TryParse( string input, out object value ) { byte parsedVal; bool result = byte.TryParse( input, NumberStyles.Integer, RuntimeInspectorUtils.numberFormat, out parsedVal ); value = parsedVal; return result; }
			public bool ValuesAreEqual( object value1, object value2 ) { return (byte) value1 == (byte) value2; }

			public object ConvertFromFloat( float value ) { return (byte) value; }
			public float ConvertToFloat( object value ) { return (byte) value; }

			public string ToString( object value ) { return ( (byte) value ).ToString( RuntimeInspectorUtils.numberFormat ); }
		}

		private class SByteHandler : INumberHandler
		{
			public float MinValue { get { return sbyte.MinValue; } }
			public float MaxValue { get { return sbyte.MaxValue; } }

			public bool TryParse( string input, out object value ) { sbyte parsedVal; bool result = sbyte.TryParse( input, NumberStyles.Integer, RuntimeInspectorUtils.numberFormat, out parsedVal ); value = parsedVal; return result; }
			public bool ValuesAreEqual( object value1, object value2 ) { return (sbyte) value1 == (sbyte) value2; }

			public object ConvertFromFloat( float value ) { return (sbyte) value; }
			public float ConvertToFloat( object value ) { return (sbyte) value; }

			public string ToString( object value ) { return ( (sbyte) value ).ToString( RuntimeInspectorUtils.numberFormat ); }
		}

		private class ShortHandler : INumberHandler
		{
			public float MinValue { get { return short.MinValue; } }
			public float MaxValue { get { return short.MaxValue; } }

			public bool TryParse( string input, out object value ) { short parsedVal; bool result = short.TryParse( input, NumberStyles.Integer, RuntimeInspectorUtils.numberFormat, out parsedVal ); value = parsedVal; return result; }
			public bool ValuesAreEqual( object value1, object value2 ) { return (short) value1 == (short) value2; }

			public object ConvertFromFloat( float value ) { return (short) value; }
			public float ConvertToFloat( object value ) { return (short) value; }

			public string ToString( object value ) { return ( (short) value ).ToString( RuntimeInspectorUtils.numberFormat ); }
		}

		private class UShortHandler : INumberHandler
		{
			public float MinValue { get { return ushort.MinValue; } }
			public float MaxValue { get { return ushort.MaxValue; } }

			public bool TryParse( string input, out object value ) { ushort parsedVal; bool result = ushort.TryParse( input, NumberStyles.Integer, RuntimeInspectorUtils.numberFormat, out parsedVal ); value = parsedVal; return result; }
			public bool ValuesAreEqual( object value1, object value2 ) { return (ushort) value1 == (ushort) value2; }

			public object ConvertFromFloat( float value ) { return (ushort) value; }
			public float ConvertToFloat( object value ) { return (ushort) value; }

			public string ToString( object value ) { return ( (ushort) value ).ToString( RuntimeInspectorUtils.numberFormat ); }
		}

		private class CharHandler : INumberHandler
		{
			public float MinValue { get { return char.MinValue; } }
			public float MaxValue { get { return char.MaxValue; } }

			public bool TryParse( string input, out object value ) { char parsedVal; bool result = char.TryParse( input, out parsedVal ); value = parsedVal; return result; }
			public bool ValuesAreEqual( object value1, object value2 ) { return (char) value1 == (char) value2; }

			public object ConvertFromFloat( float value ) { return (char) value; }
			public float ConvertToFloat( object value ) { return (char) value; }

			public string ToString( object value ) { return ( (char) value ).ToString( RuntimeInspectorUtils.numberFormat ); }
		}

		private class FloatHandler : INumberHandler
		{
			public float MinValue { get { return float.MinValue; } }
			public float MaxValue { get { return float.MaxValue; } }

			public bool TryParse( string input, out object value ) { float parsedVal; bool result = float.TryParse( input, NumberStyles.Float, RuntimeInspectorUtils.numberFormat, out parsedVal ); value = parsedVal; return result; }
			public bool ValuesAreEqual( object value1, object value2 ) { return (float) value1 == (float) value2; }

			public object ConvertFromFloat( float value ) { return value; }
			public float ConvertToFloat( object value ) { return (float) value; }

			public string ToString( object value ) { return ( (float) value ).ToString( RuntimeInspectorUtils.numberFormat ); }
		}

		private class DoubleHandler : INumberHandler
		{
			public float MinValue { get { return float.MinValue; } }
			public float MaxValue { get { return float.MaxValue; } }

			public bool TryParse( string input, out object value ) { double parsedVal; bool result = double.TryParse( input, NumberStyles.Float, RuntimeInspectorUtils.numberFormat, out parsedVal ); value = parsedVal; return result; }
			public bool ValuesAreEqual( object value1, object value2 ) { return (double) value1 == (double) value2; }

			public object ConvertFromFloat( float value ) { return (double) value; }
			public float ConvertToFloat( object value ) { return (float) (double) value; }

			public string ToString( object value ) { return ( (double) value ).ToString( RuntimeInspectorUtils.numberFormat ); }
		}

		private class DecimalHandler : INumberHandler
		{
			public float MinValue { get { return float.MinValue; } }
			public float MaxValue { get { return float.MaxValue; } }

			public bool TryParse( string input, out object value ) { decimal parsedVal; bool result = decimal.TryParse( input, NumberStyles.Float, RuntimeInspectorUtils.numberFormat, out parsedVal ); value = parsedVal; return result; }
			public bool ValuesAreEqual( object value1, object value2 ) { return (decimal) value1 == (decimal) value2; }

			public object ConvertFromFloat( float value ) { return (decimal) value; }
			public float ConvertToFloat( object value ) { return (float) (decimal) value; }

			public string ToString( object value ) { return ( (decimal) value ).ToString( RuntimeInspectorUtils.numberFormat ); }
		}
		#endregion

		private static readonly Dictionary<Type, INumberHandler> handlers = new Dictionary<Type, INumberHandler>( 16 );

		public static INumberHandler Get( Type type )
		{
			INumberHandler result;
			if( !handlers.TryGetValue( type, out result ) )
			{
				if( type == typeof( int ) )
					result = new IntHandler();
				else if( type == typeof( float ) )
					result = new FloatHandler();
				else if( type == typeof( long ) )
					result = new LongHandler();
				else if( type == typeof( double ) )
					result = new DoubleHandler();
				else if( type == typeof( byte ) )
					result = new ByteHandler();
				else if( type == typeof( char ) )
					result = new CharHandler();
				else if( type == typeof( short ) )
					result = new ShortHandler();
				else if( type == typeof( uint ) )
					result = new UIntHandler();
				else if( type == typeof( ulong ) )
					result = new ULongHandler();
				else if( type == typeof( sbyte ) )
					result = new SByteHandler();
				else if( type == typeof( ushort ) )
					result = new UShortHandler();
				else if( type == typeof( decimal ) )
					result = new DecimalHandler();
				else
					result = null;

				handlers[type] = result;
			}

			return result;
		}
	}
}