// Copyright (c) 2015, Felix Kate All rights reserved.
// Usage of this code is governed by a BSD-style license that can be found in the LICENSE file.

/*<Description>
For the new Unity GUI System won't work on older Unity Versions.
Short script for handling the controls of the color picker GUI element.
The user can drag the slider on the ring to change the hue and the slider in the box to set the blackness and saturation.
If used without prefab add this to an image canvas element which useses the ColorWheelMaterial.
Also needs 2 subobjects with images as slider graphics and an even trigger for clicking that references the OnClick() method of this script.
*/

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

		private const int NON_EXISTING_TOUCH = -98456;

		private bool initialized = false;

		//Output Color
		private Color m_color;
		public Color Color
		{
			get { return m_color; }
			private set
			{
				if( m_color != value )
				{
					m_color = value;
					m_color.a = alpha;

					if( OnColorChanged != null )
						OnColorChanged( m_color );
				}
			}
		}

		[System.NonSerialized]
		public float alpha = 1f;

		private RectTransform rectTransform;
		private Image img;

		[SerializeField]
		private RectTransform SelectorOut;

		[SerializeField]
		private RectTransform SelectorIn;

		[SerializeField]
		private RuntimeInspectorNamespace.WindowDragHandler colorPickerWindow;

		//Control values
		private float outer;
		private Vector2 inner;

		private bool draggingOuter, draggingInner;

		//The Components of the wheel
		private Material mat;

		private float halfSize, halfSizeSqr, outerCirclePaddingSqr, innerSquareHalfSize;

		private int pointerId = NON_EXISTING_TOUCH;

		public delegate void OnColorChangedDelegate( Color32 color );
		public event OnColorChangedDelegate OnColorChanged;

		//Set up the transforms
		private void Awake()
		{
			Initialize();

			//Set first selected value to red (0° rotation and upper right corner in the box)
			//PickColor( Color.red );
		}

		public void Initialize()
		{
			if( initialized )
				return;

			rectTransform = (RectTransform) transform;
			img = GetComponent<Image>();

			//Calculate the half size
			halfSize = rectTransform.sizeDelta.x * 0.5f;
			halfSizeSqr = halfSize * halfSize;
			outerCirclePaddingSqr = halfSizeSqr * 0.75f * 0.75f;
			innerSquareHalfSize = halfSize * 0.5f;

			//Set the material
			mat = new Material( img.material );
			img.material = mat;
		}

		void OnRectTransformDimensionsChange()
		{
			Initialize();

			//Calculate the half size
			halfSize = rectTransform.sizeDelta.x * 0.5f;
			halfSizeSqr = halfSize * halfSize;
			outerCirclePaddingSqr = halfSizeSqr * 0.75f * 0.75f;
			innerSquareHalfSize = halfSize * 0.5f;

			PickColor( Color );
		}

		//Gets called after changes
		void UpdateColor()
		{
			Color c = GetCurrentBaseColor();
			mat.SetColor( "_Color", c );

			//Add the colors of the inner box
			c = Color.Lerp( c, Color.white, inner.x );
			c = Color.Lerp( c, Color.black, inner.y );

			Color = c;
		}

		//Method for setting the picker to a given color
		public void PickColor( Color c )
		{
			alpha = c.a;

			//Get hsb color from the rgb values
			float max = Mathf.Max( c.r, c.g, c.b );
			float min = Mathf.Min( c.r, c.g, c.b );

			float hue = 0;
			float sat = ( 1 - min );

			if( max == min )
				sat = 0;

			hue = Mathf.Atan2( Mathf.Sqrt( 3 ) * ( c.g - c.b ), 2 * c.r - c.g - c.b );

			//Set the sliders
			outer = hue;
			inner.x = 1 - sat;
			inner.y = 1 - max;

			//And update them once
			Color = c;
			mat.SetColor( "_Color", GetCurrentBaseColor() );

			UpdateSelectors();
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

		public void OnPointerDown( PointerEventData eventData )
		{
			if( pointerId != NON_EXISTING_TOUCH )
				return;

			Vector2 position;
			RectTransformUtility.ScreenPointToLocalPointInRectangle( rectTransform, eventData.position, eventData.pressEventCamera, out position );

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
			pointerId = NON_EXISTING_TOUCH;
		}

		private void GetSelectedColor( Vector2 pointerPos )
		{
			if( draggingOuter )
			{
				//Drag selector of outer circle

				//Get mouse direction
				Vector2 dir = -pointerPos.normalized;

				//Calculate the radians
				outer = Mathf.Atan2( -dir.x, -dir.y );

				//And update
				UpdateColor();
			}
			else if( draggingInner )
			{
				//Drag selector of inner box

				//Get position inside the box
				Vector2 dir = -pointerPos;
				dir.x = Mathf.Clamp( dir.x, -innerSquareHalfSize, innerSquareHalfSize ) + innerSquareHalfSize;
				dir.y = Mathf.Clamp( dir.y, -innerSquareHalfSize, innerSquareHalfSize ) + innerSquareHalfSize;

				//Scale the value to 0 - 1;
				inner = dir / halfSize;

				UpdateColor();
			}

			UpdateSelectors();
		}

		private void UpdateSelectors()
		{
			//Set the selectors positions
			SelectorOut.anchoredPosition = new Vector2( Mathf.Sin( outer ) * halfSize * 0.85f, Mathf.Cos( outer ) * halfSize * 0.85f );
			SelectorIn.anchoredPosition = new Vector2( innerSquareHalfSize - inner.x * halfSize, innerSquareHalfSize - inner.y * halfSize );
		}
	}
}