using System;
using System.Globalization;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace RuntimeInspectorNamespace
{
	public class Vector2Field : InspectorField
	{
#pragma warning disable 0649
		[SerializeField]
		private BoundInputField inputX;

		[SerializeField]
		private BoundInputField inputY;

		[SerializeField]
		private Text labelX;

		[SerializeField]
		private Text labelY;
#pragma warning restore 0649

#if UNITY_2017_2_OR_NEWER
		private bool isVector2Int;
#endif

		public override void Initialize()
		{
			base.Initialize();

			inputX.Initialize();
			inputY.Initialize();

			inputX.OnValueChanged += OnValueChanged;
			inputY.OnValueChanged += OnValueChanged;

			inputX.OnValueSubmitted += OnValueSubmitted;
			inputY.OnValueSubmitted += OnValueSubmitted;

			inputX.DefaultEmptyValue = "0";
			inputY.DefaultEmptyValue = "0";
		}

		public override bool SupportsType( Type type )
		{
#if UNITY_2017_2_OR_NEWER
			if( type == typeof( Vector2Int ) )
				return true;
#endif
			return type == typeof( Vector2 );
		}

		protected override void OnBound( MemberInfo variable )
		{
			base.OnBound( variable );

#if UNITY_2017_2_OR_NEWER
			isVector2Int = BoundVariableType == typeof( Vector2Int );
			if( isVector2Int )
			{
				Vector2Int val = (Vector2Int) Value;
				inputX.Text = val.x.ToString( RuntimeInspectorUtils.numberFormat );
				inputY.Text = val.y.ToString( RuntimeInspectorUtils.numberFormat );
			}
			else
#endif
			{
				Vector2 val = (Vector2) Value;
				inputX.Text = val.x.ToString( RuntimeInspectorUtils.numberFormat );
				inputY.Text = val.y.ToString( RuntimeInspectorUtils.numberFormat );
			}
		}

		private bool OnValueChanged( BoundInputField source, string input )
		{
#if UNITY_2017_2_OR_NEWER
			if( isVector2Int )
			{
				int value;
				if( int.TryParse( input, NumberStyles.Integer, RuntimeInspectorUtils.numberFormat, out value ) )
				{
					Vector2Int val = (Vector2Int) Value;
					if( source == inputX )
						val.x = value;
					else
						val.y = value;

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
					Vector2 val = (Vector2) Value;
					if( source == inputX )
						val.x = value;
					else
						val.y = value;

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

			inputX.Skin = Skin;
			inputY.Skin = Skin;

			float inputFieldWidth = ( 1f - Skin.LabelWidthPercentage ) / 3f;
			Vector2 rightSideAnchorMin = new Vector2( Skin.LabelWidthPercentage + inputFieldWidth, 0f );
			Vector2 rightSideAnchorMax = new Vector2( Skin.LabelWidthPercentage + 2f * inputFieldWidth, 1f );
			variableNameMask.rectTransform.anchorMin = rightSideAnchorMin;
			( (RectTransform) inputX.transform ).SetAnchorMinMaxInputField( labelX.rectTransform, rightSideAnchorMin, rightSideAnchorMax );

			rightSideAnchorMin.x += inputFieldWidth;
			rightSideAnchorMax.x = 1f;
			( (RectTransform) inputY.transform ).SetAnchorMinMaxInputField( labelY.rectTransform, rightSideAnchorMin, rightSideAnchorMax );
		}

		public override void Refresh()
		{
#if UNITY_2017_2_OR_NEWER
			if( isVector2Int )
			{
				Vector2Int prevVal = (Vector2Int) Value;
				base.Refresh();
				Vector2Int val = (Vector2Int) Value;

				if( val.x != prevVal.x )
					inputX.Text = val.x.ToString( RuntimeInspectorUtils.numberFormat );
				if( val.y != prevVal.y )
					inputY.Text = val.y.ToString( RuntimeInspectorUtils.numberFormat );
			}
			else
#endif
			{
				Vector2 prevVal = (Vector2) Value;
				base.Refresh();
				Vector2 val = (Vector2) Value;

				if( val.x != prevVal.x )
					inputX.Text = val.x.ToString( RuntimeInspectorUtils.numberFormat );
				if( val.y != prevVal.y )
					inputY.Text = val.y.ToString( RuntimeInspectorUtils.numberFormat );
			}
		}
	}
}