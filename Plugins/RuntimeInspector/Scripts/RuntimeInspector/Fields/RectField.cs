using System;
using System.Globalization;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace RuntimeInspectorNamespace
{
	public class RectField : InspectorField
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
			isRectInt = BoundVariableType == typeof( RectInt );
			if( isRectInt )
			{
				RectInt val = (RectInt) Value;
				inputX.Text = val.x.ToString( RuntimeInspectorUtils.numberFormat );
				inputY.Text = val.y.ToString( RuntimeInspectorUtils.numberFormat );
				inputW.Text = val.width.ToString( RuntimeInspectorUtils.numberFormat );
				inputH.Text = val.height.ToString( RuntimeInspectorUtils.numberFormat );
			}
			else
#endif
			{
				Rect val = (Rect) Value;
				inputX.Text = val.x.ToString( RuntimeInspectorUtils.numberFormat );
				inputY.Text = val.y.ToString( RuntimeInspectorUtils.numberFormat );
				inputW.Text = val.width.ToString( RuntimeInspectorUtils.numberFormat );
				inputH.Text = val.height.ToString( RuntimeInspectorUtils.numberFormat );
			}
		}

		private bool OnValueChanged( BoundInputField source, string input )
		{
#if UNITY_2017_2_OR_NEWER
			if( isRectInt )
			{
				int value;
				if( int.TryParse( input, NumberStyles.Integer, RuntimeInspectorUtils.numberFormat, out value ) )
				{
					RectInt val = (RectInt) Value;
					if( source == inputX )
						val.x = value;
					else if( source == inputY )
						val.y = value;
					else if( source == inputW )
						val.width = value;
					else
						val.height = value;

					Value = val;
					return true;
				}
			}
			else
#endif
			{
				float value;
				if( float.TryParse( input, NumberStyles.Float, RuntimeInspectorUtils.numberFormat, out value ) )
				{
					Rect val = (Rect) Value;
					if( source == inputX )
						val.x = value;
					else if( source == inputY )
						val.y = value;
					else if( source == inputW )
						val.width = value;
					else
						val.height = value;

					Value = val;
					return true;
				}
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
#if UNITY_2017_2_OR_NEWER
			if( isRectInt )
			{
				RectInt prevVal = (RectInt) Value;
				base.Refresh();
				RectInt val = (RectInt) Value;

				if( val.x != prevVal.x )
					inputX.Text = val.x.ToString( RuntimeInspectorUtils.numberFormat );
				if( val.y != prevVal.y )
					inputY.Text = val.y.ToString( RuntimeInspectorUtils.numberFormat );
				if( val.width != prevVal.width )
					inputW.Text = val.width.ToString( RuntimeInspectorUtils.numberFormat );
				if( val.height != prevVal.height )
					inputH.Text = val.height.ToString( RuntimeInspectorUtils.numberFormat );
			}
			else
#endif
			{
				Rect prevVal = (Rect) Value;
				base.Refresh();
				Rect val = (Rect) Value;

				if( val.x != prevVal.x )
					inputX.Text = val.x.ToString( RuntimeInspectorUtils.numberFormat );
				if( val.y != prevVal.y )
					inputY.Text = val.y.ToString( RuntimeInspectorUtils.numberFormat );
				if( val.width != prevVal.width )
					inputW.Text = val.width.ToString( RuntimeInspectorUtils.numberFormat );
				if( val.height != prevVal.height )
					inputH.Text = val.height.ToString( RuntimeInspectorUtils.numberFormat );
			}
		}
	}
}