﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace RuntimeInspectorNamespace
{
	public class ArrayField : ExpandableInspectorField<IList>, IDropHandler
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
				if( BoundValues == null )
						return 0;
				return BoundValues.SingleOrDefault().Count;
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

			isArray = m_boundVariableType.IsArray;
			elementType = isArray ? m_boundVariableType.GetElementType() : m_boundVariableType.GetGenericArguments()[0];
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
				elementsExpandedStates.Add( ( elements[i] is IExpandableInspectorField ) ? ( (IExpandableInspectorField) elements[i] ).IsExpanded : false );

			base.ClearElements();
		}

		protected override void GenerateElements()
		{
			if( BoundValues == null )
				return;

			int count = BoundValues.SingleOrDefault().Count;
			for( int i = 0; i < count; i++ )
			{
				InspectorField elementDrawer = Inspector.CreateDrawerForType( elementType, drawArea, Depth + 1 );
				if( elementDrawer == null )
					break;

					var everyIth = new List<object>();
					foreach( IList list in BoundValues )
							everyIth.Add( list[i] );

					int i_copy = i;
					string variableName = Inspector.ArrayIndicesStartAtOne ? ( ( i + 1 ) + ":" ) : ( i + ":" );
					elementDrawer.BindTo( elementType, variableName, () => everyIth.AsReadOnly(), everyNewIth =>
					{
						foreach( var ( list, newIth ) in BoundValues.Zip( everyNewIth, Tuple.Create ) )
							list[i_copy] = newIth;

						// Trigger setter
						BoundValues = BoundValues;
					} );

				if( i < elementsExpandedStates.Count && elementsExpandedStates[i] && elementDrawer is IExpandableInspectorField )
					( (IExpandableInspectorField) elementDrawer ).IsExpanded = true;

				elementDrawer.NameRaw = Inspector.ArrayIndicesStartAtOne ? ( ( i + 1 ) + ":" ) : ( i + ":" );
				elements.Add( elementDrawer );
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

				foreach( IList list in BoundValues )
					for( int i = 0; i < assignableObjects.Length; i++ )
						list[prevLength + i] = assignableObjects[i];

				// trigger setter
				BoundValues = BoundValues;

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
			if( !int.TryParse( input, NumberStyles.Integer, RuntimeInspectorUtils.numberFormat, out int newLength )
					|| newLength < 0
					|| !( BoundValues is IList boundObjectsList ) )
				return false;

			int currLength = Length;
			if( currLength == newLength )
				return false;

			for( int j = 0; j < boundObjectsList.Count; j++ )
			{
				if( boundObjectsList[j] is Array array )
				{
					Array newArray = Array.CreateInstance( m_boundVariableType.GetElementType(), newLength );
					if( newLength > currLength )
					{
						if( array != null )
							Array.ConstrainedCopy( array, 0, newArray, 0, currLength );

						for( int i = currLength; i < newLength; i++ )
						{
							object template = GetTemplateElement( array );
							if( template != null )
								newArray.SetValue( template, i );
						}
					}
					else
						Array.ConstrainedCopy( array, 0, newArray, 0, newLength );

					boundObjectsList[j] = newArray;
				}
				else if( boundObjectsList[j] is IList list )
				{
					int deltaLength = newLength - currLength;
					if( deltaLength > 0 )
					{
						if( list == null )
						{
							list = (IList) Activator.CreateInstance( typeof( List<> ).MakeGenericType( m_boundVariableType.GetGenericArguments()[0] ) );
							boundObjectsList[j] = list;
						}

						for( int i = 0; i < deltaLength; i++ )
							list.Add( GetTemplateElement( list ) );
					}
					else
					{
						for( int i = 0; i > deltaLength; i-- )
							list.RemoveAt( list.Count - 1 );
					}
				}
			}

			// trigger setter
			BoundValues = BoundValues;
			Inspector.RefreshDelayed();
			return true;
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
			Type elementType = isArray ? m_boundVariableType.GetElementType() : m_boundVariableType.GetGenericArguments()[0];
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
