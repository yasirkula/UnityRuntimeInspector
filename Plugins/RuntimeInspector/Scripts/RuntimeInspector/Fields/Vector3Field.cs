using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace RuntimeInspectorNamespace
{
	public class Vector3Field : InspectorField<Vector3>
#if UNITY_2017_2_OR_NEWER
	, IBound<Vector3Int>
#endif
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

		IReadOnlyList<Vector3Int> IBound<Vector3Int>.BoundValues
			=> BoundValues.Select( Vector3Int.FloorToInt );
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
			inputZ.HasMultipleValues = !coords[2].HasValue;

#if UNITY_2017_2_OR_NEWER
			if( isVector3Int )
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
			if( coords[2].HasValue )
				inputZ.Text = coords[2].Value.ToString( RuntimeInspectorUtils.numberFormat );
		}

		private bool OnValueChanged( BoundInputField source, string input )
		{
			bool couldParse;
			float value;

#if UNITY_2017_2_OR_NEWER
			if( isVector3Int )
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

			int coord;
			if( source == inputX )
					coord = 0;
			else if( source == inputY )
					coord = 1;
			else
					coord = 2;

			var newVs = new List<Vector3>();
			foreach( Vector3 oldV in BoundValues )
			{
				Vector3 newV = oldV;
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
			base.Refresh();
			UpdateInputs();
		}
	}
}
