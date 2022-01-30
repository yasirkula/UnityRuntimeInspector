using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace RuntimeInspectorNamespace
{
	public class Vector2Field : InspectorField<Vector2>
#if UNITY_2017_2_OR_NEWER
	, IBound<Vector2Int>
#endif
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

		IReadOnlyList<Vector2Int> IBound<Vector2Int>.BoundValues
		{
			get
			{
				return BoundValues.Select( Vector2Int.FloorToInt );
			}
		}
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
			isVector2Int = m_boundVariableType == typeof( Vector2Int );
#endif
			UpdateInputs();
		}

		private void UpdateInputs()
		{
			float?[] coords = BoundValues
				.Select( RuntimeInspectorUtils.ToArray )
				.SinglePerEntry();

			inputX.HasMultipleValues = !coords[0].HasValue;
			inputY.HasMultipleValues = !coords[1].HasValue;

#if UNITY_2017_2_OR_NEWER
			if( isVector2Int )
				UpdateInputTexts( coords.Cast<float?, int?>() );
			else
#endif
				UpdateInputTexts( coords );
		}

		private void UpdateInputTexts<T>( IReadOnlyList<T?> coords ) where T : struct, IConvertible
		{
			if( coords[0].HasValue )
				inputX.Text = coords[0].Value.ToString( RuntimeInspectorUtils.numberFormat );
			if( coords[1].HasValue )
				inputY.Text = coords[1].Value.ToString( RuntimeInspectorUtils.numberFormat );
		}

		private bool OnValueChanged( BoundInputField source, string input )
		{
			bool couldParse;
			float value;

#if UNITY_2017_2_OR_NEWER
			if( isVector2Int )
			{
					int intval;
					couldParse = int.TryParse( input, NumberStyles.Integer, RuntimeInspectorUtils.numberFormat, out intval );
					value = intval;
			}
			else
#endif
			couldParse = float.TryParse( input, NumberStyles.Float, RuntimeInspectorUtils.numberFormat, out value );

			if( !couldParse )
					return false;

			int coord = source == inputX ? 0 : 1;
			var newVs = new List<Vector2>();

			foreach( Vector2 oldV in BoundValues )
			{
				Vector2 newV = oldV;
				newV[coord] = value;
				newVs.Add( newV );
			}

			BoundValues = newVs;
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
			base.Refresh();
			UpdateInputs();
		}
	}
}
