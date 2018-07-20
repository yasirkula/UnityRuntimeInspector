// Copyright (c) 2015, Felix Kate All rights reserved.
// Usage of this code is governed by a BSD-style license that can be found in the LICENSE file.

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RuntimeInspectorNamespace
{
	public class ColorWheelControl : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
	{
		private const float RGB_CONST = 2 / Mathf.PI;
		private const float G_CONST = 2 * Mathf.PI * ( 1.0f / 3.0f );
		private const float B_CONST = 2 * Mathf.PI * ( 2.0f / 3.0f );

		private Color m_color;
		public Color Color
		{
			get { return m_color; }
			private set
			{
				if( m_color != value )
				{
					m_color = value;
					m_color.a = Alpha;

					if( OnColorChanged != null )
						OnColorChanged( m_color );
				}
			}
		}

		public float Alpha { get; set; }

		private RectTransform rectTransform;

		[SerializeField]
		private RectTransform SelectorOut;

		[SerializeField]
		private RectTransform SelectorIn;

		[SerializeField]
		private WindowDragHandler colorPickerWindow;

		private float outer;
		private Vector2 inner;
		private Material mat;
		private bool draggingOuter, draggingInner;
		private float halfSize, halfSizeSqr, outerCirclePaddingSqr, innerSquareHalfSize;

		private int pointerId = -98765;

		public delegate void OnColorChangedDelegate( Color32 color );
		public event OnColorChangedDelegate OnColorChanged;

		private void Awake()
		{
			rectTransform = (RectTransform) transform;
			Image img = GetComponent<Image>();

			mat = new Material( img.material );
			img.material = mat;

			UpdateProperties();
		}

		private void OnRectTransformDimensionsChange()
		{
			if( rectTransform == null )
				return;

			UpdateProperties();
			UpdateSelectors();
		}

		private void UpdateProperties()
		{
			halfSize = rectTransform.rect.size.x * 0.5f;
			halfSizeSqr = halfSize * halfSize;
			outerCirclePaddingSqr = halfSizeSqr * 0.75f * 0.75f;
			innerSquareHalfSize = halfSize * 0.5f;
		}

		public void PickColor( Color c )
		{
			Alpha = c.a;

			float h, s, v;
			Color.RGBToHSV( c, out h, out s, out v );

			outer = h * 2f * Mathf.PI;
			inner.x = 1 - s;
			inner.y = 1 - v;

			UpdateSelectors();

			Color = c;
			mat.SetColor( "_Color", GetCurrentBaseColor() );
		}

		public void OnPointerDown( PointerEventData eventData )
		{
			Vector2 position;
			if( !RectTransformUtility.ScreenPointToLocalPointInRectangle( rectTransform, eventData.position, eventData.pressEventCamera, out position ) )
				return;

			//Check if click was in outer circle, inner box or neither
			float distanceSqr = position.sqrMagnitude;
			if( distanceSqr <= halfSizeSqr && distanceSqr >= outerCirclePaddingSqr )
				draggingOuter = true;
			else if( Mathf.Abs( position.x ) <= innerSquareHalfSize && Mathf.Abs( position.y ) <= innerSquareHalfSize )
				draggingInner = true;
			else //Invalid touch, don't track
				return;

			GetSelectedColor( position );
			pointerId = eventData.pointerId;
		}

		public void OnDrag( PointerEventData eventData )
		{
			if( pointerId != eventData.pointerId )
			{
				eventData.pointerDrag = colorPickerWindow.gameObject;
				colorPickerWindow.OnBeginDrag( eventData );

				return;
			}

			Vector2 position;
			RectTransformUtility.ScreenPointToLocalPointInRectangle( rectTransform, eventData.position, eventData.pressEventCamera, out position );

			GetSelectedColor( position );
		}

		public void OnPointerUp( PointerEventData eventData )
		{
			if( pointerId != eventData.pointerId )
				return;

			Vector2 position;
			RectTransformUtility.ScreenPointToLocalPointInRectangle( rectTransform, eventData.position, eventData.pressEventCamera, out position );

			GetSelectedColor( position );

			draggingOuter = false;
			draggingInner = false;

			pointerId = -98765;
		}

		private void GetSelectedColor( Vector2 pointerPos )
		{
			if( draggingOuter )
			{
				Vector2 dir = -pointerPos.normalized;
				outer = Mathf.Atan2( -dir.x, -dir.y );

				UpdateColor();
			}
			else if( draggingInner )
			{
				Vector2 dir = -pointerPos;
				dir.x = Mathf.Clamp( dir.x, -innerSquareHalfSize, innerSquareHalfSize ) + innerSquareHalfSize;
				dir.y = Mathf.Clamp( dir.y, -innerSquareHalfSize, innerSquareHalfSize ) + innerSquareHalfSize;
				inner = dir / halfSize;

				UpdateColor();
			}

			UpdateSelectors();
		}

		private void UpdateColor()
		{
			Color c = GetCurrentBaseColor();
			mat.SetColor( "_Color", c );

			c = Color.Lerp( c, Color.white, inner.x );
			c = Color.Lerp( c, Color.black, inner.y );

			Color = c;
		}

		private Color GetCurrentBaseColor()
		{
			Color color = Color.white;

			//Calculation of rgb from degree with a modified 3 wave function
			//Check out http://en.wikipedia.org/wiki/File:HSV-RGB-comparison.svg to understand how it should look
			color.r = Mathf.Clamp( RGB_CONST * Mathf.Asin( Mathf.Cos( outer ) ) * 1.5f + 0.5f, 0f, 1f );
			color.g = Mathf.Clamp( RGB_CONST * Mathf.Asin( Mathf.Cos( G_CONST - outer ) ) * 1.5f + 0.5f, 0f, 1f );
			color.b = Mathf.Clamp( RGB_CONST * Mathf.Asin( Mathf.Cos( B_CONST - outer ) ) * 1.5f + 0.5f, 0f, 1f );

			return color;
		}

		private void UpdateSelectors()
		{
			SelectorOut.anchoredPosition = new Vector2( Mathf.Sin( outer ) * halfSize * 0.85f, Mathf.Cos( outer ) * halfSize * 0.85f );
			SelectorIn.anchoredPosition = new Vector2( innerSquareHalfSize - inner.x * halfSize, innerSquareHalfSize - inner.y * halfSize );
		}
	}
}