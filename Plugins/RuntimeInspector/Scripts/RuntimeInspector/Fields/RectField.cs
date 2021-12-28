using System;
using System.Globalization;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace RuntimeInspectorNamespace
{
	public class RectField : InspectorField<Rect>
	{
#pragma warning disable 0649
		[SerializeField]
		private BoundInputField inputX;

		[SerializeField]
		private BoundInputField inputY;

		[SerializeField]
		private BoundInputField inputW;

		[SerializeField]
		private BoundInputField inputH;

		[SerializeField]
		private Text labelX;

		[SerializeField]
		private Text labelY;

		[SerializeField]
		private Text labelW;

		[SerializeField]
		private Text labelH;
#pragma warning restore 0649

#if UNITY_2017_2_OR_NEWER
		private bool isRectInt;
#endif

		protected override float HeightMultiplier { get { return 2f; } }

		public override void Initialize()
		{
			base.Initialize();

			inputX.Initialize();
			inputY.Initialize();
			inputW.Initialize();
			inputH.Initialize();

			inputX.OnValueChanged += OnValueChanged;
			inputY.OnValueChanged += OnValueChanged;
			inputW.OnValueChanged += OnValueChanged;
			inputH.OnValueChanged += OnValueChanged;

			inputX.OnValueSubmitted += OnValueSubmitted;
			inputY.OnValueSubmitted += OnValueSubmitted;
			inputW.OnValueSubmitted += OnValueSubmitted;
			inputH.OnValueSubmitted += OnValueSubmitted;

			inputX.DefaultEmptyValue = "0";
			inputY.DefaultEmptyValue = "0";
			inputW.DefaultEmptyValue = "0";
			inputH.DefaultEmptyValue = "0";
		}

		public override bool SupportsType( Type type )
		{
#if UNITY_2017_2_OR_NEWER
			if( type == typeof( RectInt ) )
				return true;
#endif
			return type == typeof( Rect );
		}

		protected override void OnBound( MemberInfo variable )
		{
			base.OnBound( variable );

#if UNITY_2017_2_OR_NEWER
			isRectInt = m_boundVariableType == typeof( RectInt );
#endif

			inputX.Text = BoundValues.x.ToString( RuntimeInspectorUtils.numberFormat );
			inputY.Text = BoundValues.y.ToString( RuntimeInspectorUtils.numberFormat );
			inputW.Text = BoundValues.width.ToString( RuntimeInspectorUtils.numberFormat );
			inputH.Text = BoundValues.height.ToString( RuntimeInspectorUtils.numberFormat );
		}

		private bool OnValueChanged( BoundInputField source, string input )
		{
			bool success;
			float value;

#if UNITY_2017_2_OR_NEWER
			if( isRectInt )
			{
					success = int.TryParse( input, NumberStyles.Integer, RuntimeInspectorUtils.numberFormat, out int intval );
					value = intval;
			}
			else
#endif
			success = float.TryParse( input, NumberStyles.Float, RuntimeInspectorUtils.numberFormat, out value );

			if( success )
			{
				Rect val = BoundValues;
				if( source == inputX )
					val.x = value;
				else if( source == inputY )
					val.y = value;
				else if( source == inputW )
					val.width = value;
				else
					val.height = value;

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
			labelW.SetSkinText( Skin );
			labelH.SetSkinText( Skin );

			inputX.Skin = Skin;
			inputY.Skin = Skin;
			inputW.Skin = Skin;
			inputH.Skin = Skin;

			float inputFieldWidth = ( 1f - Skin.LabelWidthPercentage ) / 3f;
			Vector2 rightSideAnchorMin = new Vector2( Skin.LabelWidthPercentage + inputFieldWidth, 0f );
			Vector2 rightSideAnchorMax = new Vector2( Skin.LabelWidthPercentage + 2f * inputFieldWidth, 1f );
			variableNameMask.rectTransform.anchorMin = rightSideAnchorMin;
			( (RectTransform) inputX.transform ).SetAnchorMinMaxInputField( labelX.rectTransform, new Vector2( rightSideAnchorMin.x, 0.5f ), rightSideAnchorMax );
			( (RectTransform) inputW.transform ).SetAnchorMinMaxInputField( labelW.rectTransform, rightSideAnchorMin, new Vector2( rightSideAnchorMax.x, 0.5f ) );

			rightSideAnchorMin.x += inputFieldWidth;
			rightSideAnchorMax.x = 1f;
			( (RectTransform) inputY.transform ).SetAnchorMinMaxInputField( labelY.rectTransform, new Vector2( rightSideAnchorMin.x, 0.5f ), rightSideAnchorMax );
			( (RectTransform) inputH.transform ).SetAnchorMinMaxInputField( labelH.rectTransform, rightSideAnchorMin, new Vector2( rightSideAnchorMax.x, 0.5f ) );
		}

		public override void Refresh()
		{
				Rect prevVal = BoundValues;
				base.Refresh();

#if UNITY_2017_2_OR_NEWER
			if( isRectInt )
			{
				if( BoundValues.x != prevVal.x )
					inputX.Text = ( (int) BoundValues.x ).ToString( RuntimeInspectorUtils.numberFormat );
				if( BoundValues.y != prevVal.y )
					inputY.Text = ( (int) BoundValues.y ).ToString( RuntimeInspectorUtils.numberFormat );
				if( BoundValues.width != prevVal.width )
					inputW.Text = ( (int) BoundValues.width ).ToString( RuntimeInspectorUtils.numberFormat );
				if( BoundValues.height != prevVal.height )
					inputH.Text = ( (int) BoundValues.height ).ToString( RuntimeInspectorUtils.numberFormat );
			}
			else
#endif
			{
				if( BoundValues.x != prevVal.x )
					inputX.Text = BoundValues.x.ToString( RuntimeInspectorUtils.numberFormat );
				if( BoundValues.y != prevVal.y )
					inputY.Text = BoundValues.y.ToString( RuntimeInspectorUtils.numberFormat );
				if( BoundValues.width != prevVal.width )
					inputW.Text = BoundValues.width.ToString( RuntimeInspectorUtils.numberFormat );
				if( BoundValues.height != prevVal.height )
					inputH.Text = BoundValues.height.ToString( RuntimeInspectorUtils.numberFormat );
			}
		}
	}
}
