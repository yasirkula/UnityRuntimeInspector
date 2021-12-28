using System;
using System.Globalization;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace RuntimeInspectorNamespace
{
	public class Vector3Field : InspectorField<Vector3>
	{
#pragma warning disable 0649
		[SerializeField]
		private BoundInputField inputX;

		[SerializeField]
		private BoundInputField inputY;

		[SerializeField]
		private BoundInputField inputZ;

		[SerializeField]
		private Text labelX;

		[SerializeField]
		private Text labelY;

		[SerializeField]
		private Text labelZ;
#pragma warning restore 0649

#if UNITY_2017_2_OR_NEWER
		private bool isVector3Int;
#endif

		public override void Initialize()
		{
			base.Initialize();

			inputX.Initialize();
			inputY.Initialize();
			inputZ.Initialize();

			inputX.OnValueChanged += OnValueChanged;
			inputY.OnValueChanged += OnValueChanged;
			inputZ.OnValueChanged += OnValueChanged;

			inputX.OnValueSubmitted += OnValueSubmitted;
			inputY.OnValueSubmitted += OnValueSubmitted;
			inputZ.OnValueSubmitted += OnValueSubmitted;

			inputX.DefaultEmptyValue = "0";
			inputY.DefaultEmptyValue = "0";
			inputZ.DefaultEmptyValue = "0";
		}

		public override bool SupportsType( Type type )
		{
#if UNITY_2017_2_OR_NEWER
			if( type == typeof( Vector3Int ) )
				return true;
#endif
			return type == typeof( Vector3 );
		}

		protected override void OnBound( MemberInfo variable )
		{
			base.OnBound( variable );

#if UNITY_2017_2_OR_NEWER
			isVector3Int = m_boundVariableType == typeof( Vector3Int );
			if( isVector3Int )
			{
				inputX.Text = ( (int) BoundValues.x ).ToString( RuntimeInspectorUtils.numberFormat );
				inputY.Text = ( (int) BoundValues.y ).ToString( RuntimeInspectorUtils.numberFormat );
				inputZ.Text = ( (int) BoundValues.z ).ToString( RuntimeInspectorUtils.numberFormat );
			}
			else
#endif
			{
				inputX.Text = BoundValues.x.ToString( RuntimeInspectorUtils.numberFormat );
				inputY.Text = BoundValues.y.ToString( RuntimeInspectorUtils.numberFormat );
				inputZ.Text = BoundValues.z.ToString( RuntimeInspectorUtils.numberFormat );
			}
		}

		private bool OnValueChanged( BoundInputField source, string input )
		{
			bool success;
			float value;

#if UNITY_2017_2_OR_NEWER
			if( isVector3Int )
			{
					success = int.TryParse( input, NumberStyles.Integer, RuntimeInspectorUtils.numberFormat, out int intval );
					value = intval;
			}
			else
#endif
			success = float.TryParse( input, NumberStyles.Float, RuntimeInspectorUtils.numberFormat, out value );

			if( success )
			{
				Vector3 val = BoundValues;
				if( source == inputX )
					val.x = value;
				else if( source == inputY )
					val.y = value;
				else
					val.z = value;

				BoundValues = val;
				return true;
			}

			return false;
		}

		private bool OnValueSubmitted( BoundInputField source, string input )
		{
			Inspector.RefreshDelayed();
			return OnValueChanged( source, input );
		}

		protected override void OnSkinChanged()
		{
			base.OnSkinChanged();

			labelX.SetSkinText( Skin );
			labelY.SetSkinText( Skin );
			labelZ.SetSkinText( Skin );

			inputX.Skin = Skin;
			inputY.Skin = Skin;
			inputZ.Skin = Skin;

			float inputFieldWidth = ( 1f - Skin.LabelWidthPercentage ) / 3f;
			Vector2 rightSideAnchorMin = new Vector2( Skin.LabelWidthPercentage, 0f );
			Vector2 rightSideAnchorMax = new Vector2( Skin.LabelWidthPercentage + inputFieldWidth, 1f );
			variableNameMask.rectTransform.anchorMin = rightSideAnchorMin;
			( (RectTransform) inputX.transform ).SetAnchorMinMaxInputField( labelX.rectTransform, rightSideAnchorMin, rightSideAnchorMax );

			rightSideAnchorMin.x += inputFieldWidth;
			rightSideAnchorMax.x += inputFieldWidth;
			( (RectTransform) inputY.transform ).SetAnchorMinMaxInputField( labelY.rectTransform, rightSideAnchorMin, rightSideAnchorMax );

			rightSideAnchorMin.x += inputFieldWidth;
			rightSideAnchorMax.x = 1f;
			( (RectTransform) inputZ.transform ).SetAnchorMinMaxInputField( labelZ.rectTransform, rightSideAnchorMin, rightSideAnchorMax );
		}

		public override void Refresh()
		{
				Vector3 prevVal = BoundValues;
				base.Refresh();

#if UNITY_2017_2_OR_NEWER
			if( isVector3Int )
			{
				if( BoundValues.x != prevVal.x )
					inputX.Text = ( (int) BoundValues.x ).ToString( RuntimeInspectorUtils.numberFormat );
				if( BoundValues.y != prevVal.y )
					inputY.Text = ( (int) BoundValues.y ).ToString( RuntimeInspectorUtils.numberFormat );
				if( BoundValues.z != prevVal.z )
					inputZ.Text = ( (int) BoundValues.z ).ToString( RuntimeInspectorUtils.numberFormat );
			}
			else
#endif
			{
				if( BoundValues.x != prevVal.x )
					inputX.Text = BoundValues.x.ToString( RuntimeInspectorUtils.numberFormat );
				if( BoundValues.y != prevVal.y )
					inputY.Text = BoundValues.y.ToString( RuntimeInspectorUtils.numberFormat );
				if( BoundValues.z != prevVal.z )
					inputZ.Text = BoundValues.z.ToString( RuntimeInspectorUtils.numberFormat );
			}
		}
	}
}
