using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Visibility = RuntimeInspectorNamespace.RuntimeInspector.VariableVisibility;

namespace RuntimeInspectorNamespace
{
	public class ExposedVariablesEnumerator : IEnumerator<MemberInfo>, IEnumerable<MemberInfo>
	{
		public MemberInfo Current { get { return variables[index]; } }
		object IEnumerator.Current { get { return variables[index]; } }

		private int index;

		private readonly MemberInfo[] variables;
		private readonly List<VariableSet> hiddenVariables, exposedVariables;

		private readonly Visibility fieldVisibility, propertyVisibility;

		public ExposedVariablesEnumerator( MemberInfo[] variables, List<VariableSet> hiddenVariables, List<VariableSet> exposedVariables, Visibility fieldVisibility, Visibility propertyVisibility )
		{
			index = -1;

			this.variables = variables;

			this.hiddenVariables = hiddenVariables;
			this.exposedVariables = exposedVariables;

			this.fieldVisibility = fieldVisibility;
			this.propertyVisibility = propertyVisibility;
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
				if( ShouldExposeVariable( variables[index] ) )
					return true;
			}

			return false;
		}

		public void Reset()
		{
			index = -1;
		}

		private bool ShouldExposeVariable( MemberInfo variable )
		{
			string variableName = variable.Name;
			if( exposedVariables != null )
			{
				for( int i = 0; i < exposedVariables.Count; i++ )
				{
					if( exposedVariables[i].variables.Contains( variableName ) )
						return true;
				}
			}

			if( hiddenVariables != null )
			{
				for( int i = 0; i < hiddenVariables.Count; i++ )
				{
					if( hiddenVariables[i].variables.Contains( variableName ) )
						return false;
				}
			}

			if( variable is FieldInfo )
			{
				switch( fieldVisibility )
				{
					case Visibility.None: return false;
					case Visibility.All: return true;
					case Visibility.SerializableOnly:
						FieldInfo field = (FieldInfo) variable;
						return field.IsPublic || field.HasAttribute<SerializeField>();
				}
			}
			else
			{
				switch( propertyVisibility )
				{
					case Visibility.None: return false;
					case Visibility.All: return true;
					case Visibility.SerializableOnly:
						PropertyInfo property = (PropertyInfo) variable;
						return property.GetGetMethod( true ).IsPublic || property.HasAttribute<SerializeField>();
				}
			}

			return true;
		}
	}
}