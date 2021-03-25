using System;

namespace RuntimeInspectorNamespace
{
	[AttributeUsage( AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = true )]
	public class RuntimeInspectorCustomEditorAttribute : Attribute, IComparable<RuntimeInspectorCustomEditorAttribute>
	{
		private readonly Type m_inspectedType;
		private readonly bool m_editorForChildClasses;
		private readonly int m_inspectedTypeDepth;

		public Type InspectedType { get { return m_inspectedType; } }
		public bool EditorForChildClasses { get { return m_editorForChildClasses; } }

		public RuntimeInspectorCustomEditorAttribute( Type inspectedType, bool editorForChildClasses = false )
		{
			m_inspectedType = inspectedType;
			m_editorForChildClasses = editorForChildClasses;
			m_inspectedTypeDepth = 0;

			while( inspectedType != typeof( object ) )
			{
				inspectedType = inspectedType.BaseType;
				m_inspectedTypeDepth++;
			}
		}

		// While sorting a list of RuntimeInspectorCustomEditor attributes, sort them by their depths in descending order
		int IComparable<RuntimeInspectorCustomEditorAttribute>.CompareTo( RuntimeInspectorCustomEditorAttribute other )
		{
			return other.m_inspectedTypeDepth.CompareTo( m_inspectedTypeDepth );
		}
	}

	public interface IRuntimeInspectorCustomEditor
	{
		void GenerateElements( ObjectField parent );
		void Refresh();
		void Cleanup();
	}
}