using UnityEngine;

namespace RuntimeInspectorNamespace
{
	public abstract class SkinnedWindow : MonoBehaviour
	{
#pragma warning disable 0649
		[SerializeField]
		private UISkin m_skin;
		private int m_skinVersion = 0;
		public UISkin Skin
		{
			get { return m_skin; }
			set
			{
				if( value != null && m_skin != value )
				{
					m_skin = value;
					m_skinVersion = m_skin.Version - 1;
				}
			}
		}
#pragma warning restore 0649

		protected virtual void Awake()
		{
			// Refresh skin
			m_skinVersion = Skin.Version - 1;

			// Unity 2017.2 bugfix
			gameObject.SetActive( false );
			gameObject.SetActive( true );
		}

		protected virtual void Update()
		{
			if( m_skinVersion != Skin.Version )
			{
				m_skinVersion = Skin.Version;
				RefreshSkin();
			}
		}

		protected abstract void RefreshSkin();
	}
}