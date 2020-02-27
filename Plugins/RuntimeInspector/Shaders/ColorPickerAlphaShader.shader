// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "UI/ColorPickerAlpha" 
{
	Properties 
	{
		_StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15
	}

	SubShader 
	{
		Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
		
		Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
		
		Lighting Off
		ZWrite Off
		ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
	
		Pass
		{		
			CGPROGRAM
			#pragma vertex vert
	        #pragma fragment frag
			#pragma target 2.0
			
			#include "UnityCG.cginc"
			
			//Prepare the inputs
			struct vertIN
			{
				float4 vertex : POSITION;
				float4 texcoord0 : TEXCOORD0;
				fixed4 color : COLOR0;
			};
			
			struct fragIN
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				fixed4 color : COLOR0;
			};

			//Fill the vert struct
			fragIN vert (vertIN v)
			{
				fragIN o;
				
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.texcoord0;
				o.color = v.color;

				return o;
			}
			
			//Draw the circle
			fixed4 frag(fragIN i) : COLOR
			{
				fixed4 c = i.color;
				c.a = i.uv.x;

				return c;
			}
			
			ENDCG
		}
	} 
}
