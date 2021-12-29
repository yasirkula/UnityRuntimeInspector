using System;
using System.Collections.Generic;
using System.Linq;
#if !UNITY_EDITOR && NETFX_CORE
using System.Reflection;
#endif
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace RuntimeInspectorNamespace
{
	public class TextureReferenceField : ObjectReferenceField
	{
#pragma warning disable 0649
		[SerializeField]
		private RawImage referencePreview;

		[SerializeField]
		private Text multiValueText;
#pragma warning restore 0649

		protected override float HeightMultiplier { get { return 2f; } }

		public override bool SupportsType( Type type )
		{
			return typeof( Texture ).IsAssignableFrom( type ) || typeof( Sprite ).IsAssignableFrom( type );
		}

		protected override void OnSkinChanged()
		{
			base.OnSkinChanged();
			multiValueText.SetSkinInputFieldText( Skin );
		}

		protected override void OnReferenceChanged( IEnumerable<Object> references )
		{
			base.OnReferenceChanged( references );
			referenceNameText.gameObject.SetActive( !references.Any() );

			if( BoundValues.GetSingle( out Object value ) )
			{
				Texture tex = value.GetTexture();
				referencePreview.enabled = tex != null;
				referencePreview.texture = tex;
				multiValueText.enabled = false;
			}
			else
			{
				referencePreview.enabled = false;
				multiValueText.enabled = true;
			}
		}
	}
}
