= Runtime Inspector & Hierarchy =

Online documentation available at: https://github.com/yasirkula/UnityRuntimeInspector
E-mail: yasirkula@gmail.com

1. ABOUT
This asset is a simple yet powerful runtime Inspector and Hierarchy solution for Unity 3D that should work on pretty much any platform that Unity supports, including mobile platforms.

2. HOW TO
Please see the online documentation for an in-depth documentation of the Scripting API: https://github.com/yasirkula/UnityRuntimeInspector

- To use the hierarchy in your scene, drag&drop the RuntimeHierarchy prefab to your canvas
- To use the inspector in your scene, drag&drop the RuntimeInspector prefab to your canvas

You can connect the inspector to the hierarchy so that whenever the selection in the hierarchy changes, inspector inspects the newly selected object. To do this, assign the inspector to the Connected Inspector property of the hierarchy.

You can also connect the hierarchy to the inspector so that whenever an object reference in the inspector is highlighted, the selection in hierarchy is updated. To do this, assign the hierarchy to the Connected Hierarchy property of the inspector.

Note that these connections are one-directional, meaning that assigning the inspector to the hierarchy will not automatically assign the hierarchy to the inspector or vice versa. Also note that the inspector and the hierarchy are not singletons and therefore, you can have several instances of them in your scene at a time with different configurations.