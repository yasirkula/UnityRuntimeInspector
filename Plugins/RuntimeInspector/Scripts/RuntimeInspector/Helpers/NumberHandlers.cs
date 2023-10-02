using System;
using System.Collections.Generic;
using System.Globalization;

namespace RuntimeInspectorNamespace
{
	public interface INumberHandler
	{
		float MinValue { get; }
		float MaxValue { get; }

		bool TryParse( string input, out IConvertible value );

		IConvertible ConvertFromFloat( float value );
		float ConvertToFloat( IConvertible value );
	}

	public class NumberHandlers
	{
		#region Implementations
		private class IntHandler : INumberHandler
		{
			public float MinValue { get { return int.MinValue; } }
			public float MaxValue { get { return int.MaxValue; } }

			public bool TryParse( string input, out IConvertible value ) { int parsedVal; bool result = int.TryParse( input, NumberStyles.Integer, RuntimeInspectorUtils.numberFormat, out parsedVal ); value = parsedVal; return result; }

			public IConvertible ConvertFromFloat( float value ) { return (int) value; }
			public float ConvertToFloat( IConvertible value ) { return (int) value; }
		}

		private class UIntHandler : INumberHandler
		{
			public float MinValue { get { return uint.MinValue; } }
			public float MaxValue { get { return uint.MaxValue; } }

			public bool TryParse( string input, out IConvertible value ) { uint parsedVal; bool result = uint.TryParse( input, NumberStyles.Integer, RuntimeInspectorUtils.numberFormat, out parsedVal ); value = parsedVal; return result; }

			public IConvertible ConvertFromFloat( float value ) { return (uint) value; }
			public float ConvertToFloat( IConvertible value ) { return (uint) value; }
		}

		private class LongHandler : INumberHandler
		{
			public float MinValue { get { return long.MinValue; } }
			public float MaxValue { get { return long.MaxValue; } }

			public bool TryParse( string input, out IConvertible value ) { long parsedVal; bool result = long.TryParse( input, NumberStyles.Integer, RuntimeInspectorUtils.numberFormat, out parsedVal ); value = parsedVal; return result; }

			public IConvertible ConvertFromFloat( float value ) { return (long) value; }
			public float ConvertToFloat( IConvertible value ) { return (long) value; }
		}

		private class ULongHandler : INumberHandler
		{
			public float MinValue { get { return ulong.MinValue; } }
			public float MaxValue { get { return ulong.MaxValue; } }

			public bool TryParse( string input, out IConvertible value ) { ulong parsedVal; bool result = ulong.TryParse( input, NumberStyles.Integer, RuntimeInspectorUtils.numberFormat, out parsedVal ); value = parsedVal; return result; }

			public IConvertible ConvertFromFloat( float value ) { return (ulong) value; }
			public float ConvertToFloat( IConvertible value ) { return (ulong) value; }
		}

		private class ByteHandler : INumberHandler
		{
			public float MinValue { get { return byte.MinValue; } }
			public float MaxValue { get { return byte.MaxValue; } }

			public bool TryParse( string input, out IConvertible value ) { byte parsedVal; bool result = byte.TryParse( input, NumberStyles.Integer, RuntimeInspectorUtils.numberFormat, out parsedVal ); value = parsedVal; return result; }

			public IConvertible ConvertFromFloat( float value ) { return (byte) value; }
			public float ConvertToFloat( IConvertible value ) { return (byte) value; }
		}

		private class SByteHandler : INumberHandler
		{
			public float MinValue { get { return sbyte.MinValue; } }
			public float MaxValue { get { return sbyte.MaxValue; } }

			public bool TryParse( string input, out IConvertible value ) { sbyte parsedVal; bool result = sbyte.TryParse( input, NumberStyles.Integer, RuntimeInspectorUtils.numberFormat, out parsedVal ); value = parsedVal; return result; }

			public IConvertible ConvertFromFloat( float value ) { return (sbyte) value; }
			public float ConvertToFloat( IConvertible value ) { return (sbyte) value; }
		}

		private class ShortHandler : INumberHandler
		{
			public float MinValue { get { return short.MinValue; } }
			public float MaxValue { get { return short.MaxValue; } }

			public bool TryParse( string input, out IConvertible value ) { short parsedVal; bool result = short.TryParse( input, NumberStyles.Integer, RuntimeInspectorUtils.numberFormat, out parsedVal ); value = parsedVal; return result; }

			public IConvertible ConvertFromFloat( float value ) { return (short) value; }
			public float ConvertToFloat( IConvertible value ) { return (short) value; }
		}

		private class UShortHandler : INumberHandler
		{
			public float MinValue { get { return ushort.MinValue; } }
			public float MaxValue { get { return ushort.MaxValue; } }

			public bool TryParse( string input, out IConvertible value ) { ushort parsedVal; bool result = ushort.TryParse( input, NumberStyles.Integer, RuntimeInspectorUtils.numberFormat, out parsedVal ); value = parsedVal; return result; }

			public IConvertible ConvertFromFloat( float value ) { return (ushort) value; }
			public float ConvertToFloat( IConvertible value ) { return (ushort) value; }
		}

		private class CharHandler : INumberHandler
		{
			public float MinValue { get { return char.MinValue; } }
			public float MaxValue { get { return char.MaxValue; } }

			public bool TryParse( string input, out IConvertible value ) { char parsedVal; bool result = char.TryParse( input, out parsedVal ); value = parsedVal; return result; }

			public IConvertible ConvertFromFloat( float value ) { return (char) value; }
			public float ConvertToFloat( IConvertible value ) { return (char) value; }
		}

		private class FloatHandler : INumberHandler
		{
			public float MinValue { get { return float.MinValue; } }
			public float MaxValue { get { return float.MaxValue; } }

			public bool TryParse( string input, out IConvertible value ) { float parsedVal; bool result = float.TryParse( input, NumberStyles.Float, RuntimeInspectorUtils.numberFormat, out parsedVal ); value = parsedVal; return result; }

			public IConvertible ConvertFromFloat( float value ) { return value; }
			public float ConvertToFloat( IConvertible value ) { return (float) value; }
		}

		private class DoubleHandler : INumberHandler
		{
			public float MinValue { get { return float.MinValue; } }
			public float MaxValue { get { return float.MaxValue; } }

			public bool TryParse( string input, out IConvertible value ) { double parsedVal; bool result = double.TryParse( input, NumberStyles.Float, RuntimeInspectorUtils.numberFormat, out parsedVal ); value = parsedVal; return result; }

			public IConvertible ConvertFromFloat( float value ) { return (double) value; }
			public float ConvertToFloat( IConvertible value ) { return (float) (double) value; }
		}

		private class DecimalHandler : INumberHandler
		{
			public float MinValue { get { return float.MinValue; } }
			public float MaxValue { get { return float.MaxValue; } }

			public bool TryParse( string input, out IConvertible value ) { decimal parsedVal; bool result = decimal.TryParse( input, NumberStyles.Float, RuntimeInspectorUtils.numberFormat, out parsedVal ); value = parsedVal; return result; }

			public IConvertible ConvertFromFloat( float value ) { return (decimal) value; }
			public float ConvertToFloat( IConvertible value ) { return (float) (decimal) value; }
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
