using System;
using System.Collections.Generic;
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

		protected override void OnReferenceChanged( IReadOnlyList<Object> references )
		{
			base.OnReferenceChanged( references );

			Object value;
			if( BoundValues.GetSingle( out value ) )
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
