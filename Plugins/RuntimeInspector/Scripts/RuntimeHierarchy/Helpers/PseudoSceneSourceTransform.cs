using System.Collections.Generic;
using UnityEngine;

namespace RuntimeInspectorNamespace
{
	public class PseudoSceneSourceTransform : MonoBehaviour
	{
		[SerializeField]
		private RuntimeHierarchy m_hierarchy;
		public RuntimeHierarchy Hierarchy
		{
			get { return m_hierarchy; }
			set
			{
				if( m_hierarchy != value )
				{
					RemoveChildrenFromScene();
					m_hierarchy = value;
					AddChildrenToScene();
				}
			}
		}

		[SerializeField]
		private string m_sceneName;
		public string SceneName
		{
			get { return m_sceneName; }
			set
			{
				if( m_sceneName != value )
				{
					RemoveChildrenFromScene();
					m_sceneName = value;
					AddChildrenToScene();
				}
			}
		}

		[SerializeField]
		private bool m_hideOnDisable = false;
		public bool HideOnDisable
		{
			get { return m_hideOnDisable; }
			set
			{
				if( m_hideOnDisable != value )
				{
					m_hideOnDisable = value;
					if( !isEnabled )
					{
						if( value )
							RemoveChildrenFromScene();
						else
							AddChildrenToScene();
					}
				}
			}
		}

		private HashSet<Transform> childrenCurrent = new HashSet<Transform>();
		private HashSet<Transform> childrenNew = new HashSet<Transform>();

		private bool updateChildren = false;
		private bool isEnabled = true;
		private bool isQuitting = false;

		private bool ShouldUpdateChildren { get { return ( isEnabled || !m_hideOnDisable ) && !Hierarchy.IsNull() && !string.IsNullOrEmpty( m_sceneName ); } }

		private void OnEnable()
		{
			isEnabled = true;
			updateChildren = true;
		}

		private void OnDisable()
		{
			if( isQuitting )
				return;

			isEnabled = false;

			if( m_hideOnDisable )
				RemoveChildrenFromScene();
		}

		private void OnApplicationQuit()
		{
			isQuitting = true;	
		}

		private void OnTransformChildrenChanged()
		{
			updateChildren = true;
		}

		private void Update()
		{
			if( updateChildren )
			{
				updateChildren = false;

				if( !ShouldUpdateChildren )
					return;

				for( int i = 0; i < transform.childCount; i++ )
				{
					Transform child = transform.GetChild( i );
					childrenNew.Add( child );

					if( !childrenCurrent.Remove( child ) )
						Hierarchy.AddToPseudoScene( m_sceneName, child );
				}

				RemoveChildrenFromScene();
					
				HashSet<Transform> temp = childrenCurrent;
				childrenCurrent = childrenNew;
				childrenNew = temp;
			}
		}

		private void AddChildrenToScene()
		{
			if( !ShouldUpdateChildren )
				return;

			for( int i = 0; i < transform.childCount; i++ )
			{
				Transform child = transform.GetChild( i );
				if( childrenCurrent.Add( child ) )
					Hierarchy.AddToPseudoScene( m_sceneName, child );
			}
		}

		private void RemoveChildrenFromScene()
		{
			if( Hierarchy.IsNull() || string.IsNullOrEmpty( m_sceneName ) )
				return;

			foreach( Transform removedChild in childrenCurrent )
			{
				if( !removedChild.IsNull() )
					Hierarchy.RemoveFromPseudoScene( m_sceneName, removedChild, true );
			}

			childrenCurrent.Clear();
		}
	}
}