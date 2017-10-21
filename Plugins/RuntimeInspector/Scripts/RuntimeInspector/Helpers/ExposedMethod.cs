using System;
using System.Reflection;

namespace RuntimeInspectorNamespace
{
	public struct ExposedMethod
	{
		private readonly MethodInfo method;
		private readonly RuntimeInspectorButtonAttribute properties;
		private readonly bool isExtensionMethod;

		public string Label { get { return properties.Label; } }

		public bool IsInitializer { get { return properties.IsInitializer; } }

		public bool VisibleWhenInitialized { get { return ( properties.Visibility & ButtonVisibility.InitializedObjects ) == ButtonVisibility.InitializedObjects; } }
		public bool VisibleWhenUninitialized { get { return ( properties.Visibility & ButtonVisibility.UninitializedObjects ) == ButtonVisibility.UninitializedObjects; } }

		public ExposedMethod( MethodInfo method, RuntimeInspectorButtonAttribute properties, bool isExtensionMethod )
		{
			this.method = method;
			this.properties = properties;
			this.isExtensionMethod = isExtensionMethod;
		}

		public void Call( object source )
		{
			if( isExtensionMethod )
				method.Invoke( null, new object[] { source } );
			else if( method.IsStatic )
				method.Invoke( null, null );
			else if( source != null )
				method.Invoke( source, null );
		}

		public object CallAndReturnValue( object source )
		{
			if( isExtensionMethod )
				return method.Invoke( null, new object[] { source } );

			if( method.IsStatic )
				return method.Invoke( null, null );
			
			if( source != null )
				return method.Invoke( source, null );

			return null;
		}
	}

	public struct ExposedExtensionMethodHolder
	{
		public readonly Type extendedType;
		public readonly MethodInfo method;
		public readonly RuntimeInspectorButtonAttribute properties;

		public ExposedExtensionMethodHolder( Type extendedType, MethodInfo method, RuntimeInspectorButtonAttribute properties )
		{
			this.extendedType = extendedType;
			this.method = method;
			this.properties = properties;
		}
	}
}