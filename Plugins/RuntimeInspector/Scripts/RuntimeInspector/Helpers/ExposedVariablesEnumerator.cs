using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace RuntimeInspectorNamespace
{
	public class ExposedVariablesEnumerator : IEnumerator<MemberInfo>, IEnumerable<MemberInfo>
	{
		public MemberInfo Current { get { return variables[index]; } }
		object IEnumerator.Current { get { return Current; } }
		
		private int index;
		private MemberInfo[] variables;

		private List<VariableSet> hiddenVariables, exposedVariables;

		private bool debugMode;

		private bool exposePrivateFields, exposePublicFields;
		private bool exposePrivateProperties, exposePublicProperties;

		public ExposedVariablesEnumerator( MemberInfo[] variables, List<VariableSet> hiddenVariables, List<VariableSet> exposedVariables, bool debugMode,
			bool exposePrivateFields, bool exposePublicFields, bool exposePrivateProperties, bool exposePublicProperties )
		{
			index = -1;

			this.variables = variables;
			
			this.hiddenVariables = hiddenVariables;
			this.exposedVariables = exposedVariables;

			this.debugMode = debugMode;

			this.exposePrivateFields = exposePrivateFields;
			this.exposePublicFields = exposePublicFields;
			this.exposePrivateProperties = exposePrivateProperties;
			this.exposePublicProperties = exposePublicProperties;
		}
		
		public void Dispose() { }

		public IEnumerator<MemberInfo> GetEnumerator()
		{
			return this;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this;
		}

		public bool MoveNext()
		{
			if( variables == null )
				return false;

			while( ++index < variables.Length )
			{
				if( variables[index] is FieldInfo )
				{
					FieldInfo field = (FieldInfo) variables[index];
					if( IsVariableInExposedVariablesList( field.Name ) )
						return true;

					bool isPublic = field.IsPublic;
					if( ( ( isPublic && exposePublicFields ) || ( !isPublic && exposePrivateFields ) ) &&
						ShouldExposeVariable( field ) )
						return true;
				}
				else
				{
					PropertyInfo property = (PropertyInfo) variables[index];
					if( IsVariableInExposedVariablesList( property.Name ) )
						return true;

					bool isPublic = property.GetSetMethod( true ).IsPublic && property.GetGetMethod( true ).IsPublic;
					if( ( ( isPublic && exposePublicProperties ) || ( !isPublic && exposePrivateProperties ) ) &&
						ShouldExposeVariable( property ) )
						return true;
				}
			}

			return false;
		}

		public void Reset()
		{
			index = -1;
        }

		private bool IsVariableInExposedVariablesList( string variableName )
		{
			if( exposedVariables != null )
			{
				for( int i = 0; i < exposedVariables.Count; i++ )
				{
					if( exposedVariables[i].variables.Contains( variableName ) )
						return true;
				}
			}

			return false;
		}

		private bool ShouldExposeVariable( MemberInfo variable )
		{
			string variableName = variable.Name;
			if( hiddenVariables != null )
			{
				for( int i = 0; i < hiddenVariables.Count; i++ )
				{
					if( hiddenVariables[i].variables.Contains( variableName ) )
						return false;
				}
			}

			return variable.ShouldExposeInInspector( debugMode );
		}
	}
}