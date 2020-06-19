#define EXCLUDE_BACKING_FIELDS_FROM_VARIABLES

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace RuntimeInspectorNamespace
{
	public static class RuntimeInspectorUtils
	{
		private static readonly Dictionary<Type, MemberInfo[]> typeToVariables = new Dictionary<Type, MemberInfo[]>( 89 ) { { typeof( object ), null } };
		private static readonly Dictionary<Type, ExposedMethod[]> typeToExposedMethods = new Dictionary<Type, ExposedMethod[]>( 89 );

		private static readonly HashSet<Type> commonSerializableTypes = new HashSet<Type>()
		{
			typeof( string ), typeof( Vector4 ), typeof( Vector3 ), typeof( Vector2 ), typeof( Rect ),
			typeof( Quaternion ), typeof( Color ), typeof( Color32 ), typeof( LayerMask ), typeof( Bounds ),
			typeof( Matrix4x4 ), typeof( AnimationCurve ), typeof( Gradient ), typeof( RectOffset ), typeof( GUIStyle ),
			typeof( bool[] ), typeof( byte[] ), typeof( sbyte[] ), typeof( char[] ), typeof( decimal[] ),
			typeof( double[] ), typeof( float[] ), typeof( int[] ), typeof( uint[] ), typeof( long[] ),
			typeof( ulong[] ), typeof( short[] ), typeof( ushort[] ), typeof( string[] ),
			typeof( Vector4[] ), typeof( Vector3[] ), typeof( Vector2[] ), typeof( Rect[] ),
			typeof( Quaternion[] ), typeof( Color[] ), typeof( Color32[] ), typeof( LayerMask[] ), typeof( Bounds[] ),
			typeof( Matrix4x4[] ), typeof( AnimationCurve[] ), typeof( Gradient[] ), typeof( RectOffset[] ), typeof( GUIStyle[] ),
			typeof( List<bool> ), typeof( List<byte> ), typeof( List<sbyte> ), typeof( List<char> ), typeof( List<decimal> ),
			typeof( List<double> ), typeof( List<float> ), typeof( List<int> ), typeof( List<uint> ), typeof( List<long> ),
			typeof( List<ulong> ), typeof( List<short> ), typeof( List<ushort> ), typeof( List<string> ),
			typeof( List<Vector4> ), typeof( List<Vector3> ), typeof( List<Vector2> ), typeof( List<Rect> ),
			typeof( List<Quaternion> ), typeof( List<Color> ), typeof( List<Color32> ), typeof( List<LayerMask> ), typeof( List<Bounds> ),
			typeof( List<Matrix4x4> ), typeof( List<AnimationCurve> ), typeof( List<Gradient> ), typeof( List<RectOffset> ), typeof( List<GUIStyle> ),
#if UNITY_2017_2_OR_NEWER
			typeof( Vector3Int ), typeof( Vector2Int ), typeof( RectInt ), typeof( BoundsInt ),
			typeof( Vector3Int[] ), typeof( Vector2Int[] ), typeof( RectInt[] ), typeof( BoundsInt[] ),
			typeof( List<Vector3Int> ), typeof( List<Vector2Int> ), typeof( List<RectInt> ), typeof( List<BoundsInt> )
#endif
		};

		private static readonly List<MemberInfo> validVariablesList = new List<MemberInfo>( 32 );
		private static readonly List<Type> typesToSearchForVariablesList = new List<Type>( 8 );
#if EXCLUDE_BACKING_FIELDS_FROM_VARIABLES
		private static readonly List<string> propertyNamesInVariablesList = new List<string>( 32 );
#endif

		private static readonly List<ExposedMethod> exposedMethodsList = new List<ExposedMethod>( 4 );
		private static readonly List<ExposedExtensionMethodHolder> exposedExtensionMethods = new List<ExposedExtensionMethodHolder>();
		public static Type ExposedExtensionMethodsHolder { set { GetExposedExtensionMethods( value ); } }

		public static readonly HashSet<Transform> IgnoredTransformsInHierarchy = new HashSet<Transform>();

		private static Canvas popupCanvas = null;
		private static Canvas popupReferenceCanvas = null;
		private static Tooltip tooltipPopup;
		private static readonly Stack<DraggedReferenceItem> draggedReferenceItemsPool = new Stack<DraggedReferenceItem>();

		internal static readonly NumberFormatInfo numberFormat = NumberFormatInfo.GetInstance( CultureInfo.InvariantCulture );
		internal static readonly StringBuilder stringBuilder = new StringBuilder( 200 );

		public static bool IsNull( this object obj )
		{
			if( obj is Object )
				return obj == null || obj.Equals( null );

			return obj == null;
		}

		public static string ToTitleCase( this string str )
		{
			if( str == null || str.Length == 0 )
				return string.Empty;

			byte lastCharType = 1; // 0 -> lowercase, 1 -> _ (underscore), 2 -> number, 3 -> uppercase
			int index = 0;
			if( str.Length > 1 && str[1] == '_' )
				index = 2;

			stringBuilder.Length = 0;
			for( ; index < str.Length; index++ )
			{
				char ch = str[index];
				if( char.IsUpper( ch ) )
				{
					if( ( lastCharType < 2 || ( str.Length > index + 1 && char.IsLower( str[index + 1] ) ) ) && stringBuilder.Length > 0 )
						stringBuilder.Append( ' ' );

					stringBuilder.Append( ch );
					lastCharType = 3;
				}
				else if( ch == '_' )
				{
					lastCharType = 1;
				}
				else if( char.IsNumber( ch ) )
				{
					if( lastCharType != 2 && stringBuilder.Length > 0 )
						stringBuilder.Append( ' ' );

					stringBuilder.Append( ch );
					lastCharType = 2;
				}
				else
				{
					if( lastCharType == 1 || lastCharType == 2 )
					{
						if( stringBuilder.Length > 0 )
							stringBuilder.Append( ' ' );

						stringBuilder.Append( char.ToUpper( ch ) );
					}
					else
						stringBuilder.Append( ch );

					lastCharType = 0;
				}
			}

			if( stringBuilder.Length == 0 )
				return str;

			return stringBuilder.ToString();
		}

		public static string GetName( this Object obj )
		{
			if( !obj )
				return "None";

			return obj.name;
		}

		public static string GetNameWithType( this object obj, Type defaultType = null )
		{
			if( obj.IsNull() )
			{
				if( defaultType == null )
					return "None";

				return string.Concat( "None (", defaultType.Name, ")" );
			}

			return ( obj is Object ) ? string.Concat( ( (Object) obj ).name, " (", obj.GetType().Name, ")" ) : obj.GetType().Name;
		}

		public static Texture GetTexture( this Object obj )
		{
			if( obj )
			{
				if( obj is Texture )
					return (Texture) obj;
				else if( obj is Sprite )
					return ( (Sprite) obj ).texture;
			}

			return null;
		}

		public static Color Tint( this Color color, float tintAmount )
		{
			if( color.r + color.g + color.b > 1.5f )
			{
				color.r -= tintAmount;
				color.g -= tintAmount;
				color.b -= tintAmount;
			}
			else
			{
				color.r += tintAmount;
				color.g += tintAmount;
				color.b += tintAmount;
			}

			return color;
		}

		public static void ShowTooltip( string tooltip, PointerEventData pointer, UISkin skin = null, Canvas referenceCanvas = null )
		{
			bool hasCanvasChanged = CreatePopupCanvas( referenceCanvas );

			if( !tooltipPopup )
			{
				tooltipPopup = (Tooltip) Object.Instantiate( Resources.Load<Tooltip>( "RuntimeInspector/Tooltip" ), popupCanvas.transform, false );
				hasCanvasChanged = true;
			}
			else
				tooltipPopup.gameObject.SetActive( true );

			if( hasCanvasChanged )
				tooltipPopup.Initialize( popupCanvas );

			if( skin )
				tooltipPopup.Skin = skin;

			tooltipPopup.SetContent( tooltip, pointer );
		}

		public static void HideTooltip()
		{
			if( tooltipPopup && tooltipPopup.gameObject.activeSelf )
				tooltipPopup.gameObject.SetActive( false );
		}

		public static DraggedReferenceItem CreateDraggedReferenceItem( Object reference, PointerEventData draggingPointer, UISkin skin = null, Canvas referenceCanvas = null )
		{
			bool hasCanvasChanged = CreatePopupCanvas( referenceCanvas );

			DraggedReferenceItem referenceItem;
			if( draggedReferenceItemsPool.Count > 0 )
			{
				referenceItem = draggedReferenceItemsPool.Pop();
				referenceItem.gameObject.SetActive( true );
			}
			else
			{
				referenceItem = (DraggedReferenceItem) Object.Instantiate( Resources.Load<DraggedReferenceItem>( "RuntimeInspector/DraggedReferenceItem" ), popupCanvas.transform, false );
				hasCanvasChanged = true;
			}

			if( hasCanvasChanged )
				referenceItem.Initialize( popupCanvas );

			if( skin )
				referenceItem.Skin = skin;

			referenceItem.SetContent( reference, draggingPointer );

			draggingPointer.dragging = true;
			draggingPointer.eligibleForClick = false;

			return referenceItem;
		}

		public static void PoolDraggedReferenceItem( DraggedReferenceItem item )
		{
			item.gameObject.SetActive( false );
			draggedReferenceItemsPool.Push( item );
		}

		public static Object GetAssignableObjectFromDraggedReferenceItem( PointerEventData draggingPointer, Type assignableType )
		{
			if( !draggingPointer.pointerDrag )
				return null;

			DraggedReferenceItem draggedReference = draggingPointer.pointerDrag.GetComponent<DraggedReferenceItem>();
			if( draggedReference && draggedReference.Reference )
			{
				if( assignableType.IsAssignableFrom( draggedReference.Reference.GetType() ) )
					return draggedReference.Reference;
				else if( typeof( Component ).IsAssignableFrom( assignableType ) )
				{
					Object component = null;
					if( draggedReference.Reference is Component )
						component = ( (Component) draggedReference.Reference ).GetComponent( assignableType );
					else if( draggedReference.Reference is GameObject )
						component = ( (GameObject) draggedReference.Reference ).GetComponent( assignableType );

					if( component )
						return component;
				}
				else if( typeof( GameObject ).IsAssignableFrom( assignableType ) )
				{
					if( draggedReference.Reference is Component )
						return ( (Component) draggedReference.Reference ).gameObject;
				}
			}

			return null;
		}

		public static void CopyValuesFrom( this Canvas canvas, Canvas referenceCanvas )
		{
			if( !canvas || !referenceCanvas )
				return;

			canvas.pixelPerfect = referenceCanvas.pixelPerfect;
			canvas.renderMode = referenceCanvas.renderMode;
			canvas.sortingLayerID = referenceCanvas.sortingLayerID;
			canvas.sortingOrder = referenceCanvas.sortingOrder;
			switch( referenceCanvas.renderMode )
			{
				case RenderMode.ScreenSpaceCamera:
					canvas.worldCamera = referenceCanvas.worldCamera;
					canvas.planeDistance = referenceCanvas.planeDistance * 0.75f;
					break;
				case RenderMode.WorldSpace:
					canvas.worldCamera = referenceCanvas.worldCamera;

					RectTransform referenceCanvasTransform = (RectTransform) referenceCanvas.transform;
					Vector3 position;
					if( referenceCanvasTransform.pivot == new Vector2( 0.5f, 0.5f ) )
						position = referenceCanvasTransform.position;
					else
					{
						Rect referenceCanvasRect = referenceCanvasTransform.rect;
						Vector3 centerOffset = new Vector3( ( 0.5f - referenceCanvasTransform.pivot.x ) * referenceCanvasRect.width, ( 0.5f - referenceCanvasTransform.pivot.y ) * referenceCanvasRect.height, 0f );
						position = referenceCanvasTransform.TransformPoint( centerOffset );
					}

#if UNITY_5_6_OR_NEWER
					canvas.transform.SetPositionAndRotation( position, referenceCanvasTransform.rotation );
#else
					canvas.transform.position = position;
					canvas.transform.rotation = referenceCanvasTransform.rotation;
#endif
					canvas.transform.localScale = referenceCanvasTransform.localScale;
					break;
			}

			CanvasScaler canvasScaler = canvas.GetComponent<CanvasScaler>();
			CanvasScaler referenceCanvasScaler = referenceCanvas.GetComponent<CanvasScaler>();
			if( !canvasScaler || !referenceCanvasScaler )
				return;

			canvasScaler.referencePixelsPerUnit = referenceCanvasScaler.referencePixelsPerUnit;

			if( referenceCanvas.renderMode == RenderMode.WorldSpace )
				canvasScaler.dynamicPixelsPerUnit = referenceCanvasScaler.dynamicPixelsPerUnit;
			else
			{
				canvasScaler.uiScaleMode = referenceCanvasScaler.uiScaleMode;
				switch( referenceCanvasScaler.uiScaleMode )
				{
					case CanvasScaler.ScaleMode.ConstantPixelSize:
						canvasScaler.scaleFactor = referenceCanvasScaler.scaleFactor;
						break;
					case CanvasScaler.ScaleMode.ScaleWithScreenSize:
						canvasScaler.referenceResolution = referenceCanvasScaler.referenceResolution;
						canvasScaler.screenMatchMode = referenceCanvasScaler.screenMatchMode;
						canvasScaler.matchWidthOrHeight = referenceCanvasScaler.matchWidthOrHeight;
						break;
					case CanvasScaler.ScaleMode.ConstantPhysicalSize:
						canvasScaler.physicalUnit = referenceCanvasScaler.physicalUnit;
						canvasScaler.fallbackScreenDPI = referenceCanvasScaler.fallbackScreenDPI;
						canvasScaler.defaultSpriteDPI = referenceCanvasScaler.defaultSpriteDPI;
						break;
				}
			}
		}

		private static bool CreatePopupCanvas( Canvas referenceCanvas )
		{
			bool hasCanvasChanged = !popupCanvas;
			if( !popupCanvas )
			{
				popupCanvas = new GameObject( "PopupCanvas" ).AddComponent<Canvas>();
				popupCanvas.gameObject.AddComponent<CanvasScaler>();

				if( !referenceCanvas )
				{
					popupCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
					popupCanvas.sortingOrder = 987654;
				}

				SceneManager.sceneLoaded -= OnSceneLoaded;
				SceneManager.sceneLoaded += OnSceneLoaded;

				Object.DontDestroyOnLoad( popupCanvas.gameObject );
				IgnoredTransformsInHierarchy.Add( popupCanvas.transform );
			}

			if( referenceCanvas && referenceCanvas != popupReferenceCanvas )
			{
				popupReferenceCanvas = referenceCanvas;

				popupCanvas.CopyValuesFrom( referenceCanvas );
				popupCanvas.sortingOrder = Mathf.Max( 987654, referenceCanvas.sortingOrder + 100 );

				hasCanvasChanged = true;
			}

			if( hasCanvasChanged )
			{
				// This makes sure that the popupCanvas is rebuilt immediately, somehow LayoutRebuilder.ForceRebuildLayoutImmediate doesn't work here
				popupCanvas.gameObject.SetActive( false );
				popupCanvas.gameObject.SetActive( true );
			}

			return hasCanvasChanged;
		}

		private static void OnSceneLoaded( Scene arg0, LoadSceneMode arg1 )
		{
			if( popupCanvas )
			{
				Transform canvasTR = popupCanvas.transform;
				for( int i = canvasTR.childCount - 1; i >= 0; i-- )
					Object.Destroy( canvasTR.GetChild( i ).gameObject );
			}
		}

		public static bool IsPointerValid( this PointerEventData eventData )
		{
			for( int i = Input.touchCount - 1; i >= 0; i-- )
			{
				if( Input.GetTouch( i ).fingerId == eventData.pointerId )
					return true;
			}

			return Input.GetMouseButton( (int) eventData.button );
		}

		public static MemberInfo[] GetAllVariables( this Type type )
		{
			MemberInfo[] result;
			if( typeToVariables.TryGetValue( type, out result ) )
				return result;

			validVariablesList.Clear();
			typesToSearchForVariablesList.Clear();

			// Follow the class hiearchy for this Type up to System.Object until some cached variables are found
			Type currType = type;
			while( currType != typeof( object ) )
			{
				// Variables for currType were already cached, no need to search currType or its base classes
				if( typeToVariables.TryGetValue( currType, out result ) )
				{
					if( result != null )
						validVariablesList.AddRange( result );

					break;
				}

				typesToSearchForVariablesList.Add( currType );
				currType = currType.BaseType;
			}

			// Fetch variables in reverse order, i.e. start from base classes
			for( int i = typesToSearchForVariablesList.Count - 1; i >= 0; i-- )
			{
				currType = typesToSearchForVariablesList[i];

#if EXCLUDE_BACKING_FIELDS_FROM_VARIABLES
				propertyNamesInVariablesList.Clear();
#endif

				PropertyInfo[] properties = currType.GetProperties( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly );
				for( int j = 0; j < properties.Length; j++ )
				{
					PropertyInfo property = properties[j];

					// Skip properties without a getter or setter function
					MethodInfo propertyGetter = property.GetGetMethod( true );
					if( propertyGetter == null || property.GetSetMethod( true ) == null )
						continue;

					// Skip indexer properties
					if( property.GetIndexParameters().Length > 0 )
						continue;

					// Skip non-serializable types
					if( !property.PropertyType.IsSerializable() )
						continue;

					// Skip obsolete or hidden properties
					if( property.HasAttribute<ObsoleteAttribute>() || property.HasAttribute<NonSerializedAttribute>() || property.HasAttribute<HideInInspector>() )
						continue;

					// Skip properties with 'override' keyword (they will appear in parent currTypes)
					if( propertyGetter.GetBaseDefinition().DeclaringType != propertyGetter.DeclaringType )
						continue;

#if EXCLUDE_BACKING_FIELDS_FROM_VARIABLES
					propertyNamesInVariablesList.Add( property.Name );
#endif
					validVariablesList.Add( property );
				}

				FieldInfo[] fields = currType.GetFields( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly );
				for( int j = 0; j < fields.Length; j++ )
				{
					FieldInfo field = fields[j];

					// Skip readonly or constant fields
					if( field.IsLiteral || field.IsInitOnly )
						continue;

					// Skip non-serializable types
					if( !field.FieldType.IsSerializable() )
						continue;

					// Skip obsolete or hidden fields
					if( field.HasAttribute<ObsoleteAttribute>() || field.HasAttribute<NonSerializedAttribute>() || field.HasAttribute<HideInInspector>() )
						continue;

#if EXCLUDE_BACKING_FIELDS_FROM_VARIABLES
					// Ignore auto-generated backing fields
					string _field = field.Name;
					if( _field.Contains( "_BackingField" ) )
						continue;

					// Skip user-written prefixes in backing fields like '_' or 'm_'
					int nameStartIndex = 0;
					if( _field.Length > 1 )
					{
						if( _field.Length > 2 && _field[1] == '_' )
							nameStartIndex = 2;
						else if( _field[0] == '_' )
							nameStartIndex = 1;
					}

					// Check if a property has the same name with this field; if so, we assume that the field is a backing field
					bool isBackingField = false;
					for( int k = propertyNamesInVariablesList.Count - 1; k >= 0; k-- )
					{
						string property = propertyNamesInVariablesList[k];
						if( _field.Length - nameStartIndex != property.Length )
							continue;

						// Perform a case-insensitive comparison of the first letters
						int firstCh = _field[nameStartIndex];
						int firstCh2 = property[0];
						if( firstCh != firstCh2 )
						{
							// Try converting upper-case first letter to lower-case and vice versa
							if( firstCh + 32 != firstCh2 && firstCh - 32 != firstCh2 )
								continue;
						}

						// Check if the remaining letters are the same
						int nameIndex = 1;
						while( nameIndex < property.Length && _field[nameStartIndex + nameIndex] == property[nameIndex] )
							nameIndex++;

						if( nameIndex == property.Length )
						{
							isBackingField = true;
							break;
						}
					}

					if( isBackingField )
						continue;
#endif

					validVariablesList.Add( field );
				}

				// Cache found variables along the way
				result = validVariablesList.Count > 0 ? validVariablesList.ToArray() : null;
				typeToVariables[currType] = result;
			}

			return result;
		}

		public static ExposedMethod[] GetExposedMethods( this Type type )
		{
			ExposedMethod[] result;
			if( !typeToExposedMethods.TryGetValue( type, out result ) )
			{
				MethodInfo[] methods = type.GetMethods( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static );

				exposedMethodsList.Clear();
				for( int i = 0; i < methods.Length; i++ )
				{
					if( !methods[i].HasAttribute<RuntimeInspectorButtonAttribute>() )
						continue;

					if( methods[i].GetParameters().Length != 0 )
						continue;

					RuntimeInspectorButtonAttribute attribute = methods[i].GetAttribute<RuntimeInspectorButtonAttribute>();
					if( !attribute.IsInitializer || type.IsAssignableFrom( methods[i].ReturnType ) )
						exposedMethodsList.Add( new ExposedMethod( methods[i], attribute, false ) );
				}

				for( int i = 0; i < exposedExtensionMethods.Count; i++ )
				{
					ExposedExtensionMethodHolder exposedExtensionMethod = exposedExtensionMethods[i];
					if( exposedExtensionMethod.extendedType.IsAssignableFrom( type ) )
						exposedMethodsList.Add( new ExposedMethod( exposedExtensionMethod.method, exposedExtensionMethod.properties, true ) );
				}

				if( exposedMethodsList.Count > 0 )
					result = exposedMethodsList.ToArray();
				else
					result = null;

				typeToExposedMethods[type] = result;
			}

			return result;
		}

		private static bool IsSerializable( this Type type )
		{
#if UNITY_EDITOR || !NETFX_CORE
			if( type.IsPrimitive || commonSerializableTypes.Contains( type ) || type.IsEnum )
#else
			if( type.GetTypeInfo().IsPrimitive || commonSerializableTypes.Contains( type ) || type.GetTypeInfo().IsEnum )
#endif
				return true;

			if( typeof( Object ).IsAssignableFrom( type ) )
				return true;

			if( type.IsArray )
			{
				if( type.GetArrayRank() != 1 )
					return false;

				return type.GetElementType().IsSerializable();
			}
#if UNITY_EDITOR || !NETFX_CORE
			else if( type.IsGenericType )
#else
			else if( type.GetTypeInfo().IsGenericType )
#endif
			{
				if( type.GetGenericTypeDefinition() != typeof( List<> ) )
					return false;

				return type.GetGenericArguments()[0].IsSerializable();
			}

#if UNITY_EDITOR || !NETFX_CORE
			if( Attribute.IsDefined( type, typeof( SerializableAttribute ), false ) )
#else
			if( type.GetTypeInfo().IsDefined( typeof( SerializableAttribute ), false ) )
#endif
				return true;

			return false;
		}

		public static bool HasAttribute<T>( this MemberInfo variable ) where T : Attribute
		{
#if UNITY_EDITOR || !NETFX_CORE
			return Attribute.IsDefined( variable, typeof( T ), true );
#else
			return variable.IsDefined( typeof( T ), true );
#endif
		}

		public static T GetAttribute<T>( this MemberInfo variable ) where T : Attribute
		{
#if UNITY_EDITOR || !NETFX_CORE
			return (T) Attribute.GetCustomAttribute( variable, typeof( T ), true );
#else
			return (T) variable.GetCustomAttribute( typeof( T ), true );
#endif
		}

		public static T[] GetAttributes<T>( this MemberInfo variable ) where T : Attribute
		{
#if UNITY_EDITOR || !NETFX_CORE
			return (T[]) Attribute.GetCustomAttributes( variable, typeof( T ), true );
#else
			return (T[]) variable.GetCustomAttributes( typeof( T ), true );
#endif
		}

		public static object Instantiate( this Type type )
		{
			try
			{
				if( typeof( ScriptableObject ).IsAssignableFrom( type ) )
					return ScriptableObject.CreateInstance( type );

#if UNITY_EDITOR || !NETFX_CORE
				if( type.GetConstructor( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null ) != null )
					return Activator.CreateInstance( type, true );

				return FormatterServices.GetUninitializedObject( type );
#else
				return Activator.CreateInstance( type, true );
#endif
			}
			catch
			{
				return null;
			}
		}

		// Credit: http://answers.unity3d.com/answers/239152/view.html
		public static Type GetType( string typeName )
		{
			try
			{
				// Try Type.GetType() first. This will work with types defined
				// by the Mono runtime, in the same assembly as the caller, etc.
				var type = Type.GetType( typeName );
				if( type != null )
					return type;

				// If the TypeName is a full name, then we can try loading the defining assembly directly
				if( typeName.Contains( "." ) )
				{
					// Get the name of the assembly (Assumption is that we are using 
					// fully-qualified type names)
					var assemblyName = typeName.Substring( 0, typeName.IndexOf( '.' ) );

					// Attempt to load the indicated Assembly
#if UNITY_EDITOR || !NETFX_CORE
					Assembly assembly = Assembly.Load( assemblyName );
#else
					Assembly assembly = Assembly.Load( new AssemblyName( assemblyName ) );
#endif
					if( assembly == null )
						return null;

					// Ask that assembly to return the proper Type
					type = assembly.GetType( typeName );
					if( type != null )
						return type;
				}
				else
				{
#if UNITY_EDITOR || !NETFX_CORE
					type = Assembly.Load( "UnityEngine" ).GetType( "UnityEngine." + typeName );
#else
					type = Assembly.Load( new AssemblyName( "UnityEngine" ) ).GetType( "UnityEngine." + typeName );
#endif

					if( type != null )
						return type;
				}

#if UNITY_EDITOR || !NETFX_CORE
				// Credit: https://forum.unity.com/threads/using-type-gettype-with-unity-objects.136580/#post-1799037
				// Search all assemblies for type
				foreach( Assembly assembly in AppDomain.CurrentDomain.GetAssemblies() )
				{
					foreach( Type t in assembly.GetTypes() )
					{
						if( t.Name == typeName )
							return t;
					}
				}
#endif
			}
			catch { }

			// The type just couldn't be found...
			return null;
		}

		private static void GetExposedExtensionMethods( Type type )
		{
			exposedExtensionMethods.Clear();
			typeToExposedMethods.Clear();

			MethodInfo[] methods = type.GetMethods( BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic );
			for( int i = 0; i < methods.Length; i++ )
			{
				if( !methods[i].HasAttribute<RuntimeInspectorButtonAttribute>() )
					continue;

				ParameterInfo[] parameters = methods[i].GetParameters();
				if( parameters.Length != 1 )
					continue;

				RuntimeInspectorButtonAttribute attribute = methods[i].GetAttribute<RuntimeInspectorButtonAttribute>();
				Type parameterType = parameters[0].ParameterType;
				if( !attribute.IsInitializer || parameterType.IsAssignableFrom( methods[i].ReturnType ) )
					exposedExtensionMethods.Add( new ExposedExtensionMethodHolder( parameterType, methods[i], attribute ) );
			}
		}
	}
}