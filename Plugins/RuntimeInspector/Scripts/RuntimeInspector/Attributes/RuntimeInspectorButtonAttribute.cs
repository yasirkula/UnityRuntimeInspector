using System;

namespace RuntimeInspectorNamespace
{
	public enum ButtonVisibility { None = 0, InitializedObjects = 1, UninitializedObjects = 2 }

	[AttributeUsage( AttributeTargets.Method, Inherited = true, AllowMultiple = false )]
	public class RuntimeInspectorButtonAttribute : Attribute
	{
		private string m_label;
		private bool m_isInitializer;
		private ButtonVisibility m_visibility;

		public string Label { get { return m_label; } }
		public bool IsInitializer { get { return m_isInitializer; } }
		public ButtonVisibility Visibility { get { return m_visibility; } }

		public RuntimeInspectorButtonAttribute( string label, bool isInitializer, ButtonVisibility visibility )
		{
			m_label = label;
			m_isInitializer = isInitializer;
			m_visibility = visibility;
		}
	}
}