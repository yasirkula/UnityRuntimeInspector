using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace RuntimeInspectorNamespace
{
	public class RectField : InspectorField<Rect>
#if UNITY_2017_2_OR_NEWER
	, IBound<RectInt>
#endif
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

		IEnumerable<RectInt> IBound<RectInt>.BoundValues
			=> BoundValues.Select( RuntimeInspectorUtils.FloorToInt );
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
			UpdateInputs();
		}

		private void UpdateInputs()
		{
			float?[] coords = BoundValues
				.Select( RuntimeInspectorUtils.Enumerate )
				.SinglePerEntry()
				.ToArray();

			inputX.HasMultipleValues = !coords[0].HasValue;
			inputY.HasMultipleValues = !coords[1].HasValue;
			inputW.HasMultipleValues = !coords[2].HasValue;
			inputH.HasMultipleValues = !coords[3].HasValue;

#if UNITY_2017_2_OR_NEWER
			if( isRectInt )
				UpdateInputTexts( coords.Cast<int?>().ToArray() );
#endif
			else
				UpdateInputTexts( coords );
		}

		private void UpdateInputTexts<T>( T?[] coords ) where T : struct, IConvertible
		{
			if( coords[0].HasValue )
				inputX.Text = coords[0].Value.ToString( RuntimeInspectorUtils.numberFormat );
			if( coords[1].HasValue )
				inputY.Text = coords[1].Value.ToString( RuntimeInspectorUtils.numberFormat );
			if( coords[2].HasValue )
				inputW.Text = coords[2].Value.ToString( RuntimeInspectorUtils.numberFormat );
			if( coords[3].HasValue )
				inputH.Text = coords[3].Value.ToString( RuntimeInspectorUtils.numberFormat );
		}

		private bool OnValueChanged( BoundInputField source, string input )
		{
			bool couldParse;
			float value;

#if UNITY_2017_2_OR_NEWER
			if( isRectInt )
			{
					couldParse = int.TryParse( input, NumberStyles.Integer, RuntimeInspectorUtils.numberFormat, out int intval );
					value = intval;
			}
			else
#endif
			couldParse = float.TryParse( input, NumberStyles.Float, RuntimeInspectorUtils.numberFormat, out value );

			if( !couldParse )
					return false;

			var newRs = new List<Rect>();
			foreach( Rect oldR in BoundValues )
			{
				Rect newR = oldR;

				if( source == inputX )
					newR.x = value;
				else if( source == inputY )
					newR.y = value;
				else if( source == inputW )
					newR.width = value;
				else
					newR.height = value;

				newRs.Add( newR );
			}

			BoundValues = newRs;
			return true;
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
			base.Refresh();
			UpdateInputs();
		}
	}
}
