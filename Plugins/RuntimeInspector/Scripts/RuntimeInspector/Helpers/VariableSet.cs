using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace RuntimeInspectorNamespace
{
	[Serializable]
	public class VariableSet
	{
		private const string INCLUDE_ALL_VARIABLES = "*";

#pragma warning disable 0649
		[SerializeField]
		private string m_type;
		public Type type;

		[SerializeField]
		private string[] m_variables;
		public HashSet<string> variables = null;
#pragma warning restore 0649

		public bool Init()
		{
			type = RuntimeInspectorUtils.GetType( m_type );
			if( type == null )
				return false;

			variables = new HashSet<string>();
			for( int i = 0; i < m_variables.Length; i++ )
			{
				if( m_variables[i] != INCLUDE_ALL_VARIABLES )
					variables.Add( m_variables[i] );
				else
				{
					AddAllVariablesToSet();
					break;
				}
			}

			return true;
		}

		private void AddAllVariablesToSet()
		{
			MemberInfo[] variables = type.GetAllVariables();
			if( variables != null )
			{
				for( int i = 0; i < variables.Length; i++ )
					this.variables.Add( variables[i].Name );
			}
		}
	}
}