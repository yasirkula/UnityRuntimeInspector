using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace RuntimeInspectorNamespace
{
	public class ObjectReferenceField : InspectorField<Object>, IDropHandler
	{
#pragma warning disable 0649
		[SerializeField]
		private RectTransform referencePickerArea;

		[SerializeField]
		private PointerEventListener input;

		[SerializeField]
		private PointerEventListener inspectReferenceButton;
		private Image inspectReferenceImage;

		[SerializeField]
		protected Image background;

		[SerializeField]
		protected Text referenceNameText;
#pragma warning restore 0649

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

		private void ShowReferencePicker( PointerEventData eventData )
		{
			Object[] allReferences = Resources.FindObjectsOfTypeAll( m_boundVariableType );

			ObjectReferencePicker.Instance.Skin = Inspector.Skin;
			ObjectReferencePicker.Instance.Show(
				( reference ) => OnReferenceChanged( (Object) reference ), null,
				( reference ) => (Object) reference ? ( (Object) reference ).name : "None",
				( reference ) => reference.GetNameWithType(),
				allReferences, BoundValues, true, "Select " + m_boundVariableType.Name, Inspector.Canvas );
		}

		private void InspectReference( PointerEventData eventData )
		{
			if( BoundValues != null && !BoundValues.Equals( null ) )
			{
				if( BoundValues is Component )
					Inspector.InspectInternal( ( (Component) BoundValues ).gameObject );
				else
					Inspector.InspectInternal( BoundValues );
			}
		}

		protected override void OnBound( MemberInfo variable )
		{
			base.OnBound( variable );
			OnReferenceChanged( BoundValues );
		}

		protected virtual void OnReferenceChanged( Object reference )
		{
			if( BoundValues != reference )
				BoundValues = reference;

			if( referenceNameText != null )
				referenceNameText.text = reference.GetNameWithType( m_boundVariableType );

			if( inspectReferenceButton != null )
				inspectReferenceButton.gameObject.SetActive( BoundValues != null && !BoundValues.Equals( null ) );

			Inspector.RefreshDelayed();
		}

		public void OnDrop( PointerEventData eventData )
		{
			Object assignableObject = (Object) RuntimeInspectorUtils.GetAssignableObjectFromDraggedReferenceItem( eventData, m_boundVariableType );
			if( assignableObject )
				OnReferenceChanged( assignableObject );
		}

		protected override void OnSkinChanged()
		{
			base.OnSkinChanged();

			background.color = Skin.InputFieldNormalBackgroundColor.Tint( 0.075f );

			referenceNameText.SetSkinInputFieldText( Skin );

			referenceNameText.resizeTextMinSize = Mathf.Max( 2, Skin.FontSize - 2 );
			referenceNameText.resizeTextMaxSize = Skin.FontSize;

			if( inspectReferenceImage )
			{
				inspectReferenceImage.color = Skin.TextColor.Tint( 0.1f );
				inspectReferenceImage.GetComponent<LayoutElement>().SetWidth( Mathf.Max( Skin.LineHeight - 8, 6 ) );
			}

			if( referencePickerArea )
			{
				Vector2 rightSideAnchorMin = new Vector2( Skin.LabelWidthPercentage, 0f );
				variableNameMask.rectTransform.anchorMin = rightSideAnchorMin;
				referencePickerArea.anchorMin = rightSideAnchorMin;
			}
		}

		public override void Refresh()
		{
			Object lastValue = BoundValues;
			base.Refresh();

			if( lastValue != BoundValues )
				OnReferenceChanged( BoundValues );
		}
	}
}
