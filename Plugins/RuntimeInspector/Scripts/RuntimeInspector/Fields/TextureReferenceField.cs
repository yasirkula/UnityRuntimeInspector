using System;
using System.Collections.Generic;
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

			if( BoundValues.GetSingle( out Object value ) )
			{
				Texture tex = value.GetTexture();
				referencePreview.enabled = tex != null;
				referencePreview.texture = tex;
				referenceNameText.enabled = value == null;
				multiValueText.enabled = false;
			}
			else
			{
				referencePreview.enabled = false;
				referenceNameText.enabled = false;
				multiValueText.enabled = true;
			}
		}
	}
}
