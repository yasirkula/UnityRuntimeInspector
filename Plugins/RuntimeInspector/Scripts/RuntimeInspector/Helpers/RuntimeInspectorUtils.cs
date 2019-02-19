using System;
using System.Collections.Generic;
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
		private static readonly Dictionary<Type, MemberInfo[]> typeToVariables = new Dictionary<Type, MemberInfo[]>( 89 );
		private static readonly Dictionary<Type, ExposedMethod[]> typeToExposedMethods = new Dictionary<Type, ExposedMethod[]>( 89 );

		private static readonly HashSet<Type> serializableUnityTypes = new HashSet<Type>() { typeof( Vector2 ), typeof( Vector3 ), typeof( Vector4),
				typeof( Rect ), typeof( Quaternion ), typeof( Matrix4x4 ), typeof( Color ), typeof( Color32 ), typeof( LayerMask ),
				typeof( Bounds ), typeof( AnimationCurve ), typeof( Gradient ), typeof( RectOffset ), typeof( GUIStyle ) };

		private static readonly List<ExposedExtensionMethodHolder> exposedExtensionMethods = new List<ExposedExtensionMethodHolder>();
		public static Type ExposedExtensionMethodsHolder { set { GetExposedExtensionMethods( value ); } }

		public static readonly HashSet<Transform> IgnoredSearchEntries = new HashSet<Transform>();

		private static Canvas m_draggedReferenceItemsCanvas = null;
		public static Canvas DraggedReferenceItemsCanvas
		{
			get
			{
				if( m_draggedReferenceItemsCanvas.IsNull() )
				{
					m_draggedReferenceItemsCanvas = new GameObject( "DraggedReferencesCanvas" ).AddComponent<Canvas>();
					m_draggedReferenceItemsCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
					m_draggedReferenceItemsCanvas.sortingOrder = 987654;
					m_draggedReferenceItemsCanvas.gameObject.AddComponent<CanvasScaler>();

					SceneManager.sceneLoaded -= OnSceneLoaded;
					SceneManager.sceneLoaded += OnSceneLoaded;

					Object.DontDestroyOnLoad( m_draggedReferenceItemsCanvas.gameObject );
				}

				return m_draggedReferenceItemsCanvas;
			}
		}

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

			StringBuilder titleCaser = new StringBuilder( str.Length + 5 );
			byte lastCharType = 1; // 0 -> lowercase, 1 -> _ (underscore), 2 -> number, 3 -> uppercase
			int i = 0;
			char ch = str[0];
			if( ( ch == 'm' || ch == 'M' ) && str.Length > 1 && str[1] == '_' )
				i = 2;

			for( ; i < str.Length; i++ )
			{
				ch = str[i];
				if( char.IsUpper( ch ) )
				{
					if( ( lastCharType < 2 || ( str.Length > i + 1 && char.IsLower( str[i + 1] ) ) ) && titleCaser.Length > 0 )
						titleCaser.Append( ' ' );

					titleCaser.Append( ch );
					lastCharType = 3;
				}
				else if( ch == '_' )
				{
					lastCharType = 1;
				}
				else if( char.IsNumber( ch ) )
				{
					if( lastCharType != 2 && titleCaser.Length > 0 )
						titleCaser.Append( ' ' );

					titleCaser.Append( ch );
					lastCharType = 2;
				}
				else
				{
					if( lastCharType == 1 || lastCharType == 2 )
					{
						if( titleCaser.Length > 0 )
							titleCaser.Append( ' ' );

						titleCaser.Append( char.ToUpper( ch ) );
					}
					else
						titleCaser.Append( ch );

					lastCharType = 0;
				}
			}

			if( titleCaser.Length == 0 )
				return str;

			return titleCaser.ToString();
		}

		public static string GetName( this Object obj )
		{
			if( obj == null || obj.Equals( null ) )
				return "None";

			return obj.name;
		}

		public static string GetNameWithType( this object obj, Type defaultType = null )
		{
			if( obj.IsNull() )
			{
				if( defaultType == null )
					return "None";

				return "None (" + defaultType.Name + ")";
			}

			return ( obj is Object ) ? ( (Object) obj ).name + " (" + obj.GetType().Name + ")" : obj.GetType().Name;
		}

		public static Texture GetTexture( this Object obj )
		{
			if( obj != null && !obj.Equals( null ) )
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

		public static DraggedReferenceItem CreateDraggedReferenceItem( Object reference, PointerEventData draggingPointer, UISkin skin = null )
		{
			DraggedReferenceItem referenceItem = (DraggedReferenceItem) Object.Instantiate( Resources.Load<DraggedReferenceItem>( "RuntimeInspector/DraggedReferenceItem" ), DraggedReferenceItemsCanvas.transform, false );
			referenceItem.Initialize( DraggedReferenceItemsCanvas, reference, draggingPointer, skin );

			return referenceItem;
		}

		public static Object GetAssignableObjectFromDraggedReferenceItem( PointerEventData draggingPointer, Type assignableType )
		{
			if( draggingPointer.pointerDrag == null )
				return null;

			DraggedReferenceItem draggedReference = draggingPointer.pointerDrag.GetComponent<DraggedReferenceItem>();
			if( !draggedReference.IsNull() && !draggedReference.Reference.IsNull() )
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

					if( !component.IsNull() )
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

		private static void OnSceneLoaded( Scene arg0, LoadSceneMode arg1 )
		{
			if( !m_draggedReferenceItemsCanvas.IsNull() )
			{
				Transform canvasTR = m_draggedReferenceItemsCanvas.transform;
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
			if( !typeToVariables.TryGetValue( type, out result ) )
			{
				FieldInfo[] fields = type.GetFields( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
				PropertyInfo[] properties = type.GetProperties( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );

				int validFieldCount = 0;
				int validPropertyCount = 0;

				for( int i = 0; i < fields.Length; i++ )
				{
					FieldInfo field = fields[i];
					if( !field.IsLiteral && !field.IsInitOnly && field.FieldType.IsSerializable() )
						validFieldCount++;
				}

				for( int i = 0; i < properties.Length; i++ )
				{
					PropertyInfo property = properties[i];
					if( property.GetIndexParameters().Length == 0 && property.CanRead && property.CanWrite && property.PropertyType.IsSerializable() )
						validPropertyCount++;
				}

				int validVariableCount = validFieldCount + validPropertyCount;
				if( validVariableCount == 0 )
					result = null;
				else
				{
					result = new MemberInfo[validVariableCount];

					int j = 0;
					for( int i = 0; i < fields.Length; i++ )
					{
						FieldInfo field = fields[i];
						if( !field.IsLiteral && !field.IsInitOnly && field.FieldType.IsSerializable() )
							result[j++] = field;
					}

					for( int i = 0; i < properties.Length; i++ )
					{
						PropertyInfo property = properties[i];
						if( property.GetIndexParameters().Length == 0 && property.CanRead && property.CanWrite && property.PropertyType.IsSerializable() )
							result[j++] = property;
					}
				}

				typeToVariables[type] = result;
			}

			return result;
		}

		public static ExposedMethod[] GetExposedMethods( this Type type )
		{
			ExposedMethod[] result;
			if( !typeToExposedMethods.TryGetValue( type, out result ) )
			{
				MethodInfo[] methods = type.GetMethods( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static );
				List<ExposedMethod> exposedMethods = new List<ExposedMethod>();

				for( int i = 0; i < methods.Length; i++ )
				{
#if UNITY_EDITOR || !NETFX_CORE
					if( !Attribute.IsDefined( methods[i], typeof( RuntimeInspectorButtonAttribute ), true ) )
#else
					if( !methods[i].IsDefined( typeof( RuntimeInspectorButtonAttribute ), true ) )
#endif
						continue;

					if( methods[i].GetParameters().Length != 0 )
						continue;

#if UNITY_EDITOR || !NETFX_CORE
					RuntimeInspectorButtonAttribute attribute = (RuntimeInspectorButtonAttribute) Attribute.GetCustomAttribute( methods[i], typeof( RuntimeInspectorButtonAttribute ), true );
#else
					RuntimeInspectorButtonAttribute attribute = (RuntimeInspectorButtonAttribute) methods[i].GetCustomAttribute( typeof( RuntimeInspectorButtonAttribute ), true );
#endif
					if( !attribute.IsInitializer || type.IsAssignableFrom( methods[i].ReturnType ) )
						exposedMethods.Add( new ExposedMethod( methods[i], attribute, false ) );
				}

				for( int i = 0; i < exposedExtensionMethods.Count; i++ )
				{
					ExposedExtensionMethodHolder exposedExtensionMethod = exposedExtensionMethods[i];
					if( exposedExtensionMethod.extendedType.IsAssignableFrom( type ) )
						exposedMethods.Add( new ExposedMethod( exposedExtensionMethod.method, exposedExtensionMethod.properties, true ) );
				}

				if( exposedMethods.Count > 0 )
					result = exposedMethods.ToArray();
				else
					result = null;

				typeToExposedMethods[type] = result;
			}

			return result;
		}

		public static bool ShouldExposeInInspector( this MemberInfo variable, bool debugMode )
		{
#if UNITY_EDITOR || !NETFX_CORE
			if( Attribute.IsDefined( variable, typeof( ObsoleteAttribute ) ) )
#else
			if( variable.IsDefined( typeof( ObsoleteAttribute ) ) )
#endif
				return false;

#if UNITY_EDITOR || !NETFX_CORE
			if( Attribute.IsDefined( variable, typeof( NonSerializedAttribute ) ) )
#else
			if( variable.IsDefined( typeof( NonSerializedAttribute ) ) )
#endif
				return false;

#if UNITY_EDITOR || !NETFX_CORE
			if( Attribute.IsDefined( variable, typeof( HideInInspector ) ) )
#else
			if( variable.IsDefined( typeof( HideInInspector ) ) )
#endif
				return false;

			if( debugMode )
				return true;

			// see Serialization Rules: https://docs.unity3d.com/Manual/script-Serialization.html
			if( variable is FieldInfo )
			{
				FieldInfo field = (FieldInfo) variable;
#if UNITY_EDITOR || !NETFX_CORE
				if( !field.IsPublic && !Attribute.IsDefined( field, typeof( SerializeField ) ) )
#else
				if( !field.IsPublic && !field.IsDefined( typeof( SerializeField ) ) )
#endif
					return false;
			}

			return true;
		}

		private static bool IsSerializable( this Type type )
		{
#if UNITY_EDITOR || !NETFX_CORE
			if( type.IsPrimitive || type == typeof( string ) || type.IsEnum )
#else
			if( type.GetTypeInfo().IsPrimitive || type == typeof( string ) || type.GetTypeInfo().IsEnum )
#endif
				return true;

			if( typeof( Object ).IsAssignableFrom( type ) )
				return true;

			if( serializableUnityTypes.Contains( type ) )
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
#if UNITY_EDITOR || !NETFX_CORE
				if( !Attribute.IsDefined( methods[i], typeof( RuntimeInspectorButtonAttribute ), true ) )
#else
				if( !methods[i].IsDefined( typeof( RuntimeInspectorButtonAttribute ), true ) )
#endif
					continue;

				ParameterInfo[] parameters = methods[i].GetParameters();
				if( parameters.Length != 1 )
					continue;

#if UNITY_EDITOR || !NETFX_CORE
				RuntimeInspectorButtonAttribute attribute = (RuntimeInspectorButtonAttribute) Attribute.GetCustomAttribute( methods[i], typeof( RuntimeInspectorButtonAttribute ), true );
#else
				RuntimeInspectorButtonAttribute attribute = (RuntimeInspectorButtonAttribute) methods[i].GetCustomAttribute( typeof( RuntimeInspectorButtonAttribute ), true );
#endif
				Type parameterType = parameters[0].ParameterType;
				if( !attribute.IsInitializer || parameterType.IsAssignableFrom( methods[i].ReturnType ) )
					exposedExtensionMethods.Add( new ExposedExtensionMethodHolder( parameterType, methods[i], attribute ) );
			}
		}
	}
}