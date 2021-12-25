using UnityEngine;

namespace RuntimeInspectorNamespace
{
	[CreateAssetMenu( fileName = "Inspector Settings", menuName = "yasirkula/RuntimeInspector/Settings", order = 111 )]
	public class RuntimeInspectorSettings : ScriptableObject
	{
#pragma warning disable 0649
		[SerializeField]
		private IInspectorField[] m_standardDrawers;
		public IInspectorField[] StandardDrawers { get { return m_standardDrawers; } }

		[SerializeField]
		private IInspectorField[] m_referenceDrawers;
		public IInspectorField[] ReferenceDrawers { get { return m_referenceDrawers; } }

		[SerializeField]
		private VariableSet[] m_hiddenVariables;
		public VariableSet[] HiddenVariables { get { return m_hiddenVariables; } }

		[SerializeField]
		private VariableSet[] m_exposedVariables;
		public VariableSet[] ExposedVariables { get { return m_exposedVariables; } }
#pragma warning restore 0649
	}
}
