using System;
using System.Globalization;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace RuntimeInspectorNamespace
{
	public class Vector4Field : InspectorField
	{
#pragma warning disable 0649
		[SerializeField]
		private BoundInputField inputX;

		[SerializeField]
		private BoundInputField inputY;

		[SerializeField]
		private BoundInputField inputZ;

		[SerializeField]
		private BoundInputField inputW;

		[SerializeField]
		private Text labelX;

		[SerializeField]
		private Text labelY;

		[SerializeField]
		private Text labelZ;

		[SerializeField]
		private Text labelW;
#pragma warning restore 0649

		private bool isQuaternion;

		protected override float HeightMultiplier { get { return 2f; } }

		public override void Initialize()
		{
			base.Initialize();

			inputX.Initialize();
			inputY.Initialize();
			inputZ.Initialize();
			inputW.Initialize();

			inputX.OnValueChanged += OnValueChanged;
			inputY.OnValueChanged += OnValueChanged;
			inputZ.OnValueChanged += OnValueChanged;
			inputW.OnValueChanged += OnValueChanged;

			inputX.OnValueSubmitted += OnValueSubmitted;
			inputY.OnValueSubmitted += OnValueSubmitted;
			inputZ.OnValueSubmitted += OnValueSubmitted;
			inputW.OnValueSubmitted += OnValueSubmitted;

			inputX.DefaultEmptyValue = "0";
			inputY.DefaultEmptyValue = "0";
			inputZ.DefaultEmptyValue = "0";
			inputW.DefaultEmptyValue = "0";
		}

		public override bool SupportsType( Type type )
		{
			return type == typeof( Vector4 ) || type == typeof( Quaternion );
		}

		protected override void OnBound( MemberInfo variable )
		{
			base.OnBound( variable );

			isQuaternion = BoundVariableType == typeof( Quaternion );
			if( isQuaternion )
			{
				Quaternion val = (Quaternion) Value;
				inputX.Text = val.x.ToString( RuntimeInspectorUtils.numberFormat );
				inputY.Text = val.y.ToString( RuntimeInspectorUtils.numberFormat );
				inputZ.Text = val.z.ToString( RuntimeInspectorUtils.numberFormat );
				inputW.Text = val.z.ToString( RuntimeInspectorUtils.numberFormat );
			}
			else
			{
				Vector4 val = (Vector4) Value;
				inputX.Text = val.x.ToString( RuntimeInspectorUtils.numberFormat );
				inputY.Text = val.y.ToString( RuntimeInspectorUtils.numberFormat );
				inputZ.Text = val.z.ToString( RuntimeInspectorUtils.numberFormat );
				inputW.Text = val.z.ToString( RuntimeInspectorUtils.numberFormat );
			}
		}

		private bool OnValueChanged( BoundInputField source, string input )
		{
			float value;
			if( float.TryParse( input, NumberStyles.Float, RuntimeInspectorUtils.numberFormat, out value ) )
			{
				if( isQuaternion )
				{
					Quaternion val = (Quaternion) Value;
					if( source == inputX )
						val.x = value;
					else if( source == inputY )
						val.y = value;
					else if( source == inputZ )
						val.z = value;
					else
						val.w = value;

					Value = val;
				}
				else
				{
					Vector4 val = (Vector4) Value;
					if( source == inputX )
						val.x = value;
					else if( source == inputY )
						val.y = value;
					else if( source == inputZ )
						val.z = value;
					else
						val.w = value;

					Value = val;
				}

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
			labelW.SetSkinText( Skin );

			inputX.Skin = Skin;
			inputY.Skin = Skin;
			inputZ.Skin = Skin;
			inputW.Skin = Skin;

			float inputFieldWidth = ( 1f - Skin.LabelWidthPercentage ) / 3f;
			Vector2 rightSideAnchorMin = new Vector2( Skin.LabelWidthPercentage + inputFieldWidth, 0f );
			Vector2 rightSideAnchorMax = new Vector2( Skin.LabelWidthPercentage + 2f * inputFieldWidth, 1f );
			variableNameMask.rectTransform.anchorMin = rightSideAnchorMin;
			( (RectTransform) inputX.transform ).SetAnchorMinMaxInputField( labelX.rectTransform, new Vector2( rightSideAnchorMin.x, 0.5f ), rightSideAnchorMax );
			( (RectTransform) inputZ.transform ).SetAnchorMinMaxInputField( labelZ.rectTransform, rightSideAnchorMin, new Vector2( rightSideAnchorMax.x, 0.5f ) );

			rightSideAnchorMin.x += inputFieldWidth;
			rightSideAnchorMax.x = 1f;
			( (RectTransform) inputY.transform ).SetAnchorMinMaxInputField( labelY.rectTransform, new Vector2( rightSideAnchorMin.x, 0.5f ), rightSideAnchorMax );
			( (RectTransform) inputW.transform ).SetAnchorMinMaxInputField( labelW.rectTransform, rightSideAnchorMin, new Vector2( rightSideAnchorMax.x, 0.5f ) );
		}

		public override void Refresh()
		{
			if( isQuaternion )
			{
				Quaternion prevVal = (Quaternion) Value;
				base.Refresh();
				Quaternion val = (Quaternion) Value;

				if( val.x != prevVal.x )
					inputX.Text = val.x.ToString( RuntimeInspectorUtils.numberFormat );
				if( val.y != prevVal.y )
					inputY.Text = val.y.ToString( RuntimeInspectorUtils.numberFormat );
				if( val.z != prevVal.z )
					inputZ.Text = val.z.ToString( RuntimeInspectorUtils.numberFormat );
				if( val.w != prevVal.w )
					inputW.Text = val.z.ToString( RuntimeInspectorUtils.numberFormat );
			}
			else
			{
				Vector4 prevVal = (Vector4) Value;
				base.Refresh();
				Vector4 val = (Vector4) Value;

				if( val.x != prevVal.x )
					inputX.Text = val.x.ToString( RuntimeInspectorUtils.numberFormat );
				if( val.y != prevVal.y )
					inputY.Text = val.y.ToString( RuntimeInspectorUtils.numberFormat );
				if( val.z != prevVal.z )
					inputZ.Text = val.z.ToString( RuntimeInspectorUtils.numberFormat );
				if( val.w != prevVal.w )
					inputW.Text = val.z.ToString( RuntimeInspectorUtils.numberFormat );
			}
		}
	}
}