using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RuntimeInspectorNamespace
{
	public class GameObjectField : ExpandableInspectorField<GameObject>
	{
		protected override int Length { get { return components.Count + 4; } } // 4: active, name, tag, layer

		private Action<GameObject, bool> isActiveSetter;
		private Action<GameObject, string> nameSetter;
		private Action<GameObject, string> tagSetter;

		private Func<GameObject, bool> isActiveGetter;
		private Func<GameObject, string> nameGetter;
		private Func<GameObject, string> tagGetter;
		private PropertyInfo layerProp;

		private readonly List<List<Component>> components = new List<List<Component>>();
		private readonly HashSet<Object> expandedElements = new HashSet<Object>();

		private Type[] addComponentTypes;

		internal static ExposedMethod addComponentMethod = new ExposedMethod( typeof( GameObjectField ).GetMethod( "AddComponentButtonClicked", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance ), new RuntimeInspectorButtonAttribute( "Add Component", false, ButtonVisibility.InitializedObjects ), false );
		internal static ExposedMethod removeComponentMethod = new ExposedMethod( typeof( GameObjectField ).GetMethod( "RemoveComponentButtonClicked", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static ), new RuntimeInspectorButtonAttribute( "Remove Component", false, ButtonVisibility.InitializedObjects ), true );

		public override void Initialize()
		{
			base.Initialize();

			isActiveGetter = go => go.activeSelf;
			isActiveSetter = ( go, value ) => go.SetActive( value );

			nameGetter = go => go.name;
			nameSetter = ( go, value ) =>
			{
				go.name = value;
				NameRaw = go.GetNameWithType();

				RuntimeHierarchy hierarchy = Inspector.ConnectedHierarchy;
				if( hierarchy )
					hierarchy.RefreshNameOf( go.transform );
			};

			tagGetter = go => go.tag;
			tagSetter = ( go, value ) => go.tag = value;

			layerProp = typeof( GameObject ).GetProperty( "layer" );
		}

		protected override void OnUnbound()
		{
			base.OnUnbound();

			components.Clear();
			expandedElements.Clear();
		}

		protected override void ClearElements()
		{
			expandedElements.Clear();
			for( int i = 0; i < elements.Count; i++ )
			{
				// Don't keep track of non-expandable drawers' or destroyed components' expanded states
				if( elements[i] is IExpandableInspectorField expandable && expandable.IsExpanded )
					expandedElements.Add( elements[i] );
			}

			base.ClearElements();
		}

		protected override void GenerateElements()
		{
			CreateDrawer<bool>( "Is Active", isActiveGetter, isActiveSetter );
			StringField nameField = CreateDrawer<string>( "Name", nameGetter, nameSetter ) as StringField;
			StringField tagField = CreateDrawer<string>( "Tag", tagGetter, tagSetter ) as StringField;
			CreateDrawerForVariable( layerProp, "Layer" );

			foreach( var multiEditedComponents in components )
			{
				InspectorField drawer = CreateDrawerForComponents( multiEditedComponents );

				if( !( drawer is IExpandableInspectorField expandable ) )
					return;

				foreach( Component comp in multiEditedComponents )
				{
					if( expandedElements.Contains( comp ) )
					{
						// If one of the multi-edited components is expanded, expand their shared drawer
						expandable.IsExpanded = true;
						break;
					}
				}
			}

			if( nameField )
				nameField.SetterMode = StringField.Mode.OnSubmit;

			if( tagField )
				tagField.SetterMode = StringField.Mode.OnSubmit;

			if( Inspector.ShowAddComponentButton )
				CreateExposedMethodButton(
					addComponentMethod,
					() => new GameObjectField[] { this },
					value => { } );

			expandedElements.Clear();
		}

		private List<Component> GetFilteredComponents( GameObject go )
		{
			var comps = new List<Component>();
			go.GetComponents( comps );

			for( int i = comps.Count - 1; i >= 0; i-- )
			{
				if( !comps[i] )
					comps.RemoveAt( i );
			}

			if( Inspector.ComponentFilter != null )
				Inspector.ComponentFilter( go, comps );

			return comps;
		}

		public override void Refresh()
		{
			// Refresh components
			components.Clear();
			var lut = new Dictionary<Type, Dictionary<GameObject, Queue<Component>>>();
			int goCount = 0;

			foreach( GameObject obj in BoundValues )
			{
				if( !obj )
					continue;

				goCount++;
				foreach( Component comp in GetFilteredComponents( obj ) )
				{
					Dictionary<GameObject, Queue<Component>> dictInLut;
					Queue<Component> queueInDictInLut;
					Type compType = comp.GetType();

					if( !lut.TryGetValue( compType, out dictInLut ) )
					{
						dictInLut = new Dictionary<GameObject, Queue<Component>>();
						lut[compType] = dictInLut;
					}

					if( !dictInLut.TryGetValue( obj, out queueInDictInLut ) )
					{
						queueInDictInLut = new Queue<Component>();
						dictInLut[obj] = queueInDictInLut;
					}

					queueInDictInLut.Enqueue( comp );
				}
			}

			while( lut.Count > 0 )
			{
				var invalidTypes = new List<Type>();

				foreach( var pair in lut )
				{
					Type compType = pair.Key;
					var dictInLut = pair.Value;
					var comps = new List<Component>( goCount );
					bool compOnAllObjects = true;

					foreach( GameObject obj in BoundValues )
					{
						if( dictInLut.TryGetValue( obj, out var queue ) )
						{
							comps.Add( queue.Dequeue() );
							if( queue.Count == 0 )
								dictInLut.Remove( obj );
						}
						else
						{
							compOnAllObjects = false;
							invalidTypes.Add( compType );
							break;
						}
					}

					if( compOnAllObjects )
					{
						components.Add( comps );

						if( dictInLut.Count == 0)
							invalidTypes.Add( compType );
					}
				}

				foreach( Type type in invalidTypes )
					lut.Remove( type );
			}

			// Regenerate components' drawers, if necessary
			base.Refresh();
		}

		[UnityEngine.Scripting.Preserve] // This method is bound to addComponentMethod
		private void AddComponentButtonClicked()
		{
			if( !BoundValues.Any() )
				return;

			if( addComponentTypes == null )
			{
				List<Type> componentTypes = new List<Type>( 128 );

#if UNITY_EDITOR || !NETFX_CORE
				Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
#else
				// Common Unity assemblies
				IEnumerable<Assembly> assemblies = new HashSet<Assembly> 
				{
					typeof( Transform ).Assembly,
					typeof( RectTransform ).Assembly,
					typeof( Rigidbody ).Assembly,
					typeof( Rigidbody2D ).Assembly,
					typeof( AudioSource ).Assembly
				};
#endif
				// Search assemblies for Component types
				foreach( Assembly assembly in assemblies )
				{
#if( NET_4_6 || NET_STANDARD_2_0 ) && ( UNITY_EDITOR || !NETFX_CORE )
					if( assembly.IsDynamic )
						continue;
#endif
					try
					{
						foreach( Type type in assembly.GetExportedTypes() )
						{
							if( !typeof( Component ).IsAssignableFrom( type ) )
								continue;

#if UNITY_EDITOR || !NETFX_CORE
							if( type.IsGenericType || type.IsAbstract )
#else
							if( type.GetTypeInfo().IsGenericType || type.GetTypeInfo().IsAbstract )
#endif
								continue;

							componentTypes.Add( type );
						}
					}
					catch( NotSupportedException ) { }
					catch( System.IO.FileNotFoundException ) { }
					catch( Exception e )
					{
						Debug.LogError( "Couldn't search assembly for Component types: " + assembly.GetName().Name + "\n" + e.ToString() );
					}
				}

				addComponentTypes = componentTypes.ToArray();
			}

			ObjectReferencePicker.Instance.Skin = Inspector.Skin;
			ObjectReferencePicker.Instance.Show(
				null, x =>
				{
					if( x is Type type && Inspector )
					{
						foreach( GameObject o in BoundValues )
						{
							// Make sure that RuntimeInspector is still inspecting this GameObject
							if( Inspector.InspectedObjects.Any( o.Equals ) )
									o.AddComponent( type );
						}
						Inspector.Refresh();
					}
				},
				( type ) => ( (Type) type ).FullName,
				( type ) => ( (Type) type ).FullName,
				addComponentTypes, null, false, "Add Component", Inspector.Canvas );
		}

		[UnityEngine.Scripting.Preserve] // This method is bound to removeComponentMethod
		private static void RemoveComponentButtonClicked( InspectorField drawer )
		{
			if( drawer is ISupportsType<Component> componentDrawer )
				drawer.StartCoroutine( RemoveComponentCoroutine(
					componentDrawer.GetBoundOfType<Component>(),
					drawer.Inspector) );
		}

		private static IEnumerator RemoveComponentCoroutine( IEnumerable<Component> components, RuntimeInspector inspector )
		{
			foreach( Component component in components )
				if( component && ! ( component is Transform ) )
					Destroy( component );

			// Destroy operation doesn't take place immediately, wait for the component to be fully destroyed
			yield return null;

			inspector.Refresh();
			inspector.EnsureScrollViewIsWithinBounds(); // Scroll view's contents can get out of bounds after removing a component
		}
	}
}
