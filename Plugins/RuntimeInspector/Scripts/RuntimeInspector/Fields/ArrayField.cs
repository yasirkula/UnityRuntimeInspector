using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace RuntimeInspectorNamespace
{
	public class ArrayField : ExpandableInspectorField, IDropHandler
	{
#pragma warning disable 0649
		[SerializeField]
		private LayoutElement sizeLayoutElement;

		[SerializeField]
		private Text sizeText;

		[SerializeField]
		private BoundInputField sizeInput;
#pragma warning restore 0649

		private bool isArray;
		private Type elementType;

		private readonly List<bool> elementsExpandedStates = new List<bool>();

		protected override int Length
		{
			get
			{
				if( isArray )
				{
					Array array = (Array) Value;
					if( array != null )
						return array.Length;
				}
				else
				{
					IList list = (IList) Value;
					if( list != null )
						return list.Count;
				}

				return 0;
			}
		}

		public override void Initialize()
		{
			base.Initialize();

			sizeInput.Initialize();
			sizeInput.OnValueChanged += OnSizeInputBeingChanged;
			sizeInput.OnValueSubmitted += OnSizeChanged;
			sizeInput.DefaultEmptyValue = "0";
			sizeInput.CacheTextOnValueChange = false;
		}

		public override bool SupportsType( Type type )
		{
			return ( type.IsArray && type.GetArrayRank() == 1 ) ||
#if UNITY_EDITOR || !NETFX_CORE
				( type.IsGenericType && type.GetGenericTypeDefinition() == typeof( List<> ) );
#else
				( type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof( List<> ) );
#endif
		}

		protected override void OnBound( MemberInfo variable )
		{
			base.OnBound( variable );

			isArray = BoundVariableType.IsArray;
			elementType = isArray ? BoundVariableType.GetElementType() : BoundVariableType.GetGenericArguments()[0];
		}

		protected override void OnUnbound()
		{
			base.OnUnbound();

			sizeInput.Text = "0";
			elementsExpandedStates.Clear();
		}

		protected override void OnSkinChanged()
		{
			base.OnSkinChanged();

			sizeInput.Skin = Skin;

			sizeLayoutElement.SetHeight( Skin.LineHeight );
			sizeText.SetSkinText( Skin );

			Vector2 rightSideAnchorMin = new Vector2( Skin.LabelWidthPercentage, 0f );
			variableNameMask.rectTransform.anchorMin = rightSideAnchorMin;
			( (RectTransform) sizeInput.transform ).anchorMin = rightSideAnchorMin;
		}

		protected override void OnDepthChanged()
		{
			base.OnDepthChanged();
			sizeText.rectTransform.sizeDelta = new Vector2( -Skin.IndentAmount * ( Depth + 1 ), 0f );
		}

		protected override void ClearElements()
		{
			elementsExpandedStates.Clear();
			for( int i = 0; i < elements.Count; i++ )
				elementsExpandedStates.Add( ( elements[i] is ExpandableInspectorField ) ? ( (ExpandableInspectorField) elements[i] ).IsExpanded : false );

			base.ClearElements();
		}

		protected override void GenerateElements()
		{
			if( Value == null )
				return;

			if( isArray )
			{
				Array array = (Array) Value;
				for( int i = 0; i < array.Length; i++ )
				{
					InspectorField elementDrawer = Inspector.CreateDrawerForType( elementType, drawArea, Depth + 1 );
					if( elementDrawer == null )
						break;

					int j = i;
					elementDrawer.BindTo( elementType, string.Empty, () => ( (Array) Value ).GetValue( j ), ( value ) =>
					{
						Array _array = (Array) Value;
						_array.SetValue( value, j );
						Value = _array;
					} );

					if( i < elementsExpandedStates.Count && elementsExpandedStates[i] && elementDrawer is ExpandableInspectorField )
						( (ExpandableInspectorField) elementDrawer ).IsExpanded = true;

					elementDrawer.NameRaw = Inspector.ArrayIndicesStartAtOne ? ( ( i + 1 ) + ":" ) : ( i + ":" );
					elements.Add( elementDrawer );
				}
			}
			else
			{
				IList list = (IList) Value;
				for( int i = 0; i < list.Count; i++ )
				{
					InspectorField elementDrawer = Inspector.CreateDrawerForType( elementType, drawArea, Depth + 1 );
					if( elementDrawer == null )
						break;

					int j = i;
					string variableName = Inspector.ArrayIndicesStartAtOne ? ( ( i + 1 ) + ":" ) : ( i + ":" );
					elementDrawer.BindTo( elementType, variableName, () => ( (IList) Value )[j], ( value ) =>
					{
						IList _list = (IList) Value;
						_list[j] = value;
						Value = _list;
					} );

					if( i < elementsExpandedStates.Count && elementsExpandedStates[i] && elementDrawer is ExpandableInspectorField )
						( (ExpandableInspectorField) elementDrawer ).IsExpanded = true;

					elements.Add( elementDrawer );
				}
			}

			sizeInput.Text = Length.ToString( RuntimeInspectorUtils.numberFormat );
			elementsExpandedStates.Clear();
		}

		void IDropHandler.OnDrop( PointerEventData eventData )
		{
			object[] assignableObjects = RuntimeInspectorUtils.GetAssignableObjectsFromDraggedReferenceItem( eventData, elementType );
			if( assignableObjects != null && assignableObjects.Length > 0 )
			{
				int prevLength = Length;
				if( !OnSizeChanged( null, ( prevLength + assignableObjects.Length ).ToString( RuntimeInspectorUtils.numberFormat ) ) )
					return;

				if( isArray )
				{
					Array _array = (Array) Value;
					for( int i = 0; i < assignableObjects.Length; i++ )
						_array.SetValue( assignableObjects[i], prevLength + i );

					Value = _array;
				}
				else
				{
					IList _list = (IList) Value;
					for( int i = 0; i < assignableObjects.Length; i++ )
						_list[prevLength + i] = assignableObjects[i];

					Value = _list;
				}

				if( !IsExpanded )
					IsExpanded = true;
			}
		}

		private bool OnSizeInputBeingChanged( BoundInputField source, string input )
		{
			int value;
			if( int.TryParse( input, NumberStyles.Integer, RuntimeInspectorUtils.numberFormat, out value ) && value >= 0 )
				return true;

			return false;
		}

		private bool OnSizeChanged( BoundInputField source, string input )
		{
			int value;
			if( int.TryParse( input, NumberStyles.Integer, RuntimeInspectorUtils.numberFormat, out value ) && value >= 0 )
			{
				int currLength = Length;
				if( currLength != value )
				{
					if( isArray )
					{
						Array array = (Array) Value;
						Array newArray = Array.CreateInstance( BoundVariableType.GetElementType(), value );
						if( value > currLength )
						{
							if( array != null )
								Array.ConstrainedCopy( array, 0, newArray, 0, currLength );

							for( int i = currLength; i < value; i++ )
							{
								object template = GetTemplateElement( array );
								if( template != null )
									newArray.SetValue( template, i );
							}
						}
						else
							Array.ConstrainedCopy( array, 0, newArray, 0, value );

						Value = newArray;
					}
					else
					{
						IList list = (IList) Value;
						int deltaLength = value - currLength;
						if( deltaLength > 0 )
						{
							if( list == null )
								list = (IList) Activator.CreateInstance( typeof( List<> ).MakeGenericType( BoundVariableType.GetGenericArguments()[0] ) );

							for( int i = 0; i < deltaLength; i++ )
								list.Add( GetTemplateElement( list ) );
						}
						else
						{
							for( int i = 0; i > deltaLength; i-- )
								list.RemoveAt( list.Count - 1 );
						}

						Value = list;
					}

					Inspector.RefreshDelayed();
				}

				return true;
			}

			return false;
		}

		private object GetTemplateElement( object value )
		{
			Array array = null;
			IList list = null;
			if( isArray )
				array = (Array) value;
			else
				list = (IList) value;

			object template = null;
			Type elementType = isArray ? BoundVariableType.GetElementType() : BoundVariableType.GetGenericArguments()[0];
#if UNITY_EDITOR || !NETFX_CORE
			if( elementType.IsValueType )
#else
			if( elementType.GetTypeInfo().IsValueType )
#endif
			{
				if( isArray && array != null && array.Length > 0 )
					template = array.GetValue( array.Length - 1 );
				else if( !isArray && list != null && list.Count > 0 )
					template = list[list.Count - 1];
				else
					template = Activator.CreateInstance( elementType );
			}
			else if( typeof( Object ).IsAssignableFrom( elementType ) )
			{
				if( isArray && array != null && array.Length > 0 )
					template = array.GetValue( array.Length - 1 );
				else if( !isArray && list != null && list.Count > 0 )
					template = list[list.Count - 1];
				else
					template = null;
			}
			else if( elementType.IsArray )
				template = Array.CreateInstance( elementType, 0 );
#if UNITY_EDITOR || !NETFX_CORE
			else if( elementType.IsGenericType && elementType.GetGenericTypeDefinition() == typeof( List<> ) )
#else
			else if( elementType.GetTypeInfo().IsGenericType && elementType.GetGenericTypeDefinition() == typeof( List<> ) )
#endif
				template = Activator.CreateInstance( typeof( List<> ).MakeGenericType( elementType ) );
			else
				template = elementType.Instantiate();

			return template;
		}
	}
}