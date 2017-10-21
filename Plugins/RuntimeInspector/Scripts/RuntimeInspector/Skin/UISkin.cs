using UnityEngine;

namespace RuntimeInspectorNamespace
{
	[CreateAssetMenu( fileName = "UI Skin", menuName = "RuntimeInspector/UI Skin", order = 111 )]
	public class UISkin : ScriptableObject
	{
		private int m_version = 0;
		public int Version { get { return m_version; } }

		[ContextMenu( "Refresh UI" )]
		private void Invalidate()
		{
			m_version = Random.Range( int.MinValue, int.MaxValue );
		}

		[SerializeField]
		private Font m_font;
		public Font Font
		{
			get { return m_font; }
			set
			{
				if( m_font != value )
				{
					m_font = value;
					m_version++;
				}
			}
		}

		[SerializeField]
		private int m_fontSize = 12;
		public int FontSize
		{
			get { return m_fontSize; }
			set
			{
				if( m_fontSize != value )
				{
					m_fontSize = value;
					m_version++;
				}
			}
		}

		[SerializeField]
		private int m_lineHeight = 30;
		public int LineHeight
		{
			get { return m_lineHeight; }
			set
			{
				if( m_lineHeight != value )
				{
					m_lineHeight = value;
					m_version++;
				}
			}
		}

		[SerializeField]
		private int m_indentAmount = 12;
		public int IndentAmount
		{
			get { return m_indentAmount; }
			set
			{
				if( m_indentAmount != value )
				{
					m_indentAmount = value;
					m_version++;
				}
			}
		}

		[SerializeField]
		private Color m_windowColor = Color.grey;
		public Color WindowColor
		{
			get { return m_windowColor; }
			set
			{
				if( m_windowColor != value )
				{
					m_windowColor = value;
					m_version++;
				}
			}
		}

		[SerializeField]
		private Color m_backgroundColor = Color.grey;
		public Color BackgroundColor
		{
			get { return m_backgroundColor; }
			set
			{
				if( m_backgroundColor != value )
				{
					m_backgroundColor = value;
					m_version++;
				}
			}
		}

		[SerializeField]
		private Color m_textColor = Color.black;
		public Color TextColor
		{
			get { return m_textColor; }
			set
			{
				if( m_textColor != value )
				{
					m_textColor = value;
					m_version++;
				}
			}
		}

		[SerializeField]
		private Color m_scrollbarColor = Color.black;
		public Color ScrollbarColor
		{
			get { return m_scrollbarColor; }
			set
			{
				if( m_scrollbarColor != value )
				{
					m_scrollbarColor = value;
					m_version++;
				}
			}
		}

		[SerializeField]
		private Color m_expandArrowColor = Color.black;
		public Color ExpandArrowColor
		{
			get { return m_expandArrowColor; }
			set
			{
				if( m_expandArrowColor != value )
				{
					m_expandArrowColor = value;
					m_version++;
				}
			}
		}
		
		[SerializeField]
		private Color m_inputFieldNormalBackgroundColor = Color.white;
		public Color InputFieldNormalBackgroundColor
		{
			get { return m_inputFieldNormalBackgroundColor; }
			set
			{
				if( m_inputFieldNormalBackgroundColor != value )
				{
					m_inputFieldNormalBackgroundColor = value;
					m_version++;
				}
			}
		}

		[SerializeField]
		private Color m_inputFieldInvalidBackgroundColor = Color.red;
		public Color InputFieldInvalidBackgroundColor
		{
			get { return m_inputFieldInvalidBackgroundColor; }
			set
			{
				if( m_inputFieldInvalidBackgroundColor != value )
				{
					m_inputFieldInvalidBackgroundColor = value;
					m_version++;
				}
			}
		}

		[SerializeField]
		private Color m_inputFieldTextColor = Color.black;
		public Color InputFieldTextColor
		{
			get { return m_inputFieldTextColor; }
			set
			{
				if( m_inputFieldTextColor != value )
				{
					m_inputFieldTextColor = value;
					m_version++;
				}
			}
		}

		[SerializeField]
		private Color m_toggleCheckmarkColor = Color.black;
		public Color ToggleCheckmarkColor
		{
			get { return m_toggleCheckmarkColor; }
			set
			{
				if( m_toggleCheckmarkColor != value )
				{
					m_toggleCheckmarkColor = value;
					m_version++;
				}
			}
		}

		[SerializeField]
		private Color m_buttonBackgroundColor = Color.white;
		public Color ButtonBackgroundColor
		{
			get { return m_buttonBackgroundColor; }
			set
			{
				if( m_buttonBackgroundColor != value )
				{
					m_buttonBackgroundColor = value;
					m_version++;
				}
			}
		}

		[SerializeField]
		private Color m_buttonTextColor = Color.black;
		public Color ButtonTextColor
		{
			get { return m_buttonTextColor; }
			set
			{
				if( m_buttonTextColor != value )
				{
					m_buttonTextColor = value;
					m_version++;
				}
			}
		}
		
		[SerializeField]
		private Color m_selectedItemBackgroundColor = Color.blue;
		public Color SelectedItemBackgroundColor
		{
			get { return m_selectedItemBackgroundColor; }
			set
			{
				if( m_selectedItemBackgroundColor != value )
				{
					m_selectedItemBackgroundColor = value;
					m_version++;
				}
			}
		}

		[SerializeField]
		private Color m_selectedItemTextColor = Color.black;
		public Color SelectedItemTextColor
		{
			get { return m_selectedItemTextColor; }
			set
			{
				if( m_selectedItemTextColor != value )
				{
					m_selectedItemTextColor = value;
					m_version++;
				}
			}
		}
	}
}