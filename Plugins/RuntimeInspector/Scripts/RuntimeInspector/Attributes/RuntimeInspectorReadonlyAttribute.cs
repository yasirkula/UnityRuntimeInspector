using System;

namespace RuntimeInspectorNamespace
{
	[AttributeUsage( AttributeTargets.Field, Inherited = false, AllowMultiple = false )]
	public class RuntimeInspectorReadonlyAttribute : Attribute
	{
		private readonly InspectorField.IsReadonlyGetter m_getter = () => true;
		public InspectorField.IsReadonlyGetter Getter { get { return m_getter; } }

		public RuntimeInspectorReadonlyAttribute() {}

		public RuntimeInspectorReadonlyAttribute( Type classAroundMethod, string methodName )
		{
			var getter = Delegate.CreateDelegate(
				type: typeof( InspectorField.IsReadonlyGetter ),
				target: classAroundMethod,
				method: methodName,
				ignoreCase: false,
				throwOnBindFailure: false );

			if( getter is InspectorField.IsReadonlyGetter readonlyGetter )
				m_getter = readonlyGetter;
		}
	}
}
