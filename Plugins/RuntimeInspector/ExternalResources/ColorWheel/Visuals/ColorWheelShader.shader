// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

//Copyright (c) 2015, Felix Kate All rights reserved.
// Usage of this code is governed by a BSD-style license that can be found in the LICENSE file.

Shader "UI/ColorWheel" 
{
	Properties 
	{
		_MainTex("Dummy", 2D) = "white" { }
		_Color ("Color", Color) = (1,1,1,1)
		//_BorderWhiteness ("Border Whiteness", Range(0.0,1.0)) = 1.0
		
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
			#pragma target 2.5
			
			#include "UnityCG.cginc"
			
			//Prepare the inputs
			struct vertIN
			{
				float4 vertex : POSITION;
				float4 texcoord0 : TEXCOORD0;
			};
			
			struct fragIN
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
			};
			
			//Function for making smooth circles from gradient
			fixed smoothCircle(fixed size, fixed gradient)
			{
				fixed scaleFactor = size + 1;
				return smoothstep(0.5 - 0.0025 * scaleFactor, 0.5 + 0.0025 * scaleFactor, 1 - gradient * scaleFactor);
			}
			
			//Function for making box from gradient
			fixed smoothBox(fixed size, fixed2 gradient)
			{
				fixed scaleFactor = size * 0.5;
				fixed alpha = ceil(gradient.x - scaleFactor);
				alpha *= ceil((1 - gradient.x) - scaleFactor);
				alpha *= ceil(gradient.y - scaleFactor);
				alpha *= ceil((1 - gradient.y) - scaleFactor);
				
				return alpha;
			}
			
			//Get the values from outside
			sampler2D _MainTex;
			fixed4 _Color;
			//fixed _BorderWhiteness;

			//Fill the vert struct
			fragIN vert (vertIN v)
			{
				fragIN o;
				
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.texcoord0;
				
				return o;
			}
			
			//Draw the circle
			fixed4 frag(fragIN i) : COLOR
			{
				fixed4 c = 1;// _BorderWhiteness1;
				
				//Make the inner area of the box
				fixed2 bGrad = 1;
				bGrad.x = smoothstep(0.25, 0.75, i.uv.x);
				bGrad.y = smoothstep(0.25, 0.75, i.uv.y);
				
				fixed4 cBox = lerp(fixed4(1, 1, 1, 1), _Color, bGrad.x) * bGrad.y;
				
				//Set up PI
				fixed PI = 3.14159265359;
				fixed PI_INV = 3 / PI;

				//Circular gradient
				fixed cGrad = distance(i.uv, fixed2(0.5, 0.5));
				
				//Angle gradient
				fixed ang = atan2(1 - i.uv.x - 0.5, 1 - i.uv.y - 0.5) + PI;

				//Calculate hue
				fixed4 cWheel = 1;
				
				cWheel.r = clamp(PI_INV * asin(clamp(cos(ang), -0.99, 0.99)) + 0.5, 0, 1); // 0.01 flexibility -> fixes precision issues on WebGL
				cWheel.g = clamp(PI_INV * asin(clamp(cos(2 * PI / 3.0 - ang), -0.99, 0.99)) + 0.5, 0, 1);
				cWheel.b = clamp(PI_INV * asin(clamp(cos(4 * PI / 3.0 - ang), -0.99, 0.99)) + 0.5, 0, 1);
	
				//Calculate white part
				//fixed aWhite = 1;// smoothCircle(0.025, cGrad);
				
				//aWhite -= smoothCircle(0.37, cGrad);
				
				//aWhite += smoothBox(0.46, i.uv.xy);
				
				//c = lerp(_BorderWhiteness, 0.8, aWhite);
				
				//Add color
				fixed aCol =  smoothCircle(0.02, cGrad);
				
				aCol -= smoothCircle(0.35, cGrad);
				
				c = lerp(c, cWheel, aCol);
				
				aCol = smoothBox(0.51, i.uv.xy);
				
				c = lerp(c, cBox, aCol);
				
				//Set alpha
				fixed alpha = smoothCircle(0, cGrad);
				
				alpha -= smoothCircle(0.4, cGrad);
				
				alpha += smoothBox(0.49, i.uv.xy);

				c.a = alpha;
				
				return c;
			}
			
			ENDCG
		}
	} 
}
