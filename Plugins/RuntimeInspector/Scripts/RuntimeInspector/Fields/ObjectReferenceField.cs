#if !UNITY_EDITOR && NETFX_CORE
using System.Reflection;
#endif
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace RuntimeInspectorNamespace
{
	public class ObjectReferenceField : InspectorField, IDropHandler
	{
		[SerializeField]
		private PointerEventListener input;

		[SerializeField]
		private PointerEventListener inspectReferenceButton;
		private Image inspectReferenceImage;

		[SerializeField]
		protected Image background;

		[SerializeField]
		protected Text referenceNameText;

		public override void Initialize()
		{
			base.Initialize();

			input.PointerClick += ShowReferencePicker;

			if( inspectReferenceButton != null )
			{
				inspectReferenceButton.PointerClick += InspectReference;
				inspectReferenceImage = inspectReferenceButton.GetComponent<Image>();
            }
		}

		public override bool SupportsType( Type type )
		{
			return typeof( Object ).IsAssignableFrom( type );
		}

		private void ShowReferencePicker( PointerEventData eventData )
		{
			Object[] allReferences = Resources.FindObjectsOfTypeAll( BoundVariableType );
			ObjectReferencePicker.Instance.Show( OnReferenceChanged, BoundVariableType, allReferences, (Object) Value );
		}

		private void InspectReference( PointerEventData eventData )
		{
			if( Value != null && !Value.Equals( null ) )
			{
				if( Value is Component )
					Inspector.Inspect( ( (Component) Value ).gameObject );
				else
					Inspector.Inspect( Value );
			}
		}

		protected override void OnBound()
		{
			base.OnBound();
			OnReferenceChanged( (Object) Value );
        }

		protected virtual void OnReferenceChanged( Object reference )
		{
			if( (Object) Value != reference )
				Value = reference;

			if( referenceNameText != null )
				referenceNameText.text = reference.GetNameWithType( BoundVariableType );
			
			if( inspectReferenceButton != null )
				inspectReferenceButton.gameObject.SetActive( Value != null && !Value.Equals( null ) );
		}

		public void OnDrop( PointerEventData eventData )
		{
			Object assignableObject = RuntimeInspectorUtils.GetAssignableObjectFromDraggedReferenceItem( eventData, BoundVariableType );
			if( assignableObject != null )
				OnReferenceChanged( assignableObject );
		}

		protected override void OnSkinChanged()
		{
			base.OnSkinChanged();

			background.color = Skin.InputFieldNormalBackgroundColor.Tint( 0.075f );

			referenceNameText.SetSkinInputFieldText( Skin );

			referenceNameText.resizeTextMinSize = Mathf.Max( 2, Skin.FontSize - 2 );
			referenceNameText.resizeTextMaxSize = Skin.FontSize;

			if( inspectReferenceImage != null )
			{
				inspectReferenceImage.color = Skin.TextColor.Tint( 0.1f );
				inspectReferenceImage.GetComponent<LayoutElement>().SetWidth( Mathf.Max( Skin.LineHeight - 8, 6 ) );
            }
		}

		public override void Refresh()
		{
			object lastValue = Value;
			base.Refresh();

			if( lastValue != Value )
				OnReferenceChanged( (Object) Value );
        }
	}
}