#if !UNITY_EDITOR && NETFX_CORE
using System.Reflection;
#endif
using System;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace RuntimeInspectorNamespace
{
	public class TextureReferenceField : ObjectReferenceField
	{
		[SerializeField]
		private RawImage referencePreview;

		protected override float HeightMultiplier { get { return 2f; } }

		public override bool SupportsType( Type type )
		{
			return typeof( Texture ).IsAssignableFrom( type ) || type == typeof( Sprite );
		}

		protected override void OnReferenceChanged( Object reference )
		{
			base.OnReferenceChanged( reference );

			referenceNameText.gameObject.SetActive( reference.IsNull() );

			Texture tex = reference.GetTexture();
			referencePreview.enabled = tex != null;
			referencePreview.texture = tex;
		}
	}
}