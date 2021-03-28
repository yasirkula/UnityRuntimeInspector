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

#if UNITY_EDITOR
		private UISkin prevSkin;
#endif

		protected virtual void Awake()
		{
			// Refresh skin
			if( m_skin )
				m_skinVersion = m_skin.Version - 1;

			// Unity 2017.2 bugfix
			gameObject.SetActive( false );
			gameObject.SetActive( true );
		}

		protected virtual void Update()
		{
			if( m_skin && m_skinVersion != m_skin.Version )
			{
				m_skinVersion = m_skin.Version;
				RefreshSkin();

#if UNITY_EDITOR
				prevSkin = m_skin;
#endif
			}
		}

#if UNITY_EDITOR
		protected virtual void OnValidate()
		{
			// Refresh skin if it is changed via Unity Inspector at runtime
			if( UnityEditor.EditorApplication.isPlaying && m_skin != prevSkin )
				m_skinVersion = m_skin ? ( m_skin.Version - 1 ) : ( m_skinVersion - 1 );
		}
#endif

		protected abstract void RefreshSkin();
	}
}