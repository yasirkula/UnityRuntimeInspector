// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

//Copyright (c) 2015, Felix Kate All rights reserved.
// Usage of this code is governed by a BSD-style license that can be found in the LICENSE file.

Shader "UI/ColorPickerAlpha" {
	Properties {
	}

	SubShader {
		Lighting Off
		Blend SrcAlpha OneMinusSrcAlpha
	
		Pass{		
			CGPROGRAM
			#pragma vertex vert
	        #pragma fragment frag
			#pragma target 3.0
			
			#include "UnityCG.cginc"
			
			//Prepare the inputs
			struct vertIN{
				float4 vertex : POSITION;
				float4 texcoord0 : TEXCOORD0;
				fixed4 color : COLOR0;
			};
			
			struct fragIN{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				fixed4 color : COLOR0;
			};

			//Fill the vert struct
			fragIN vert (vertIN v){
				fragIN o;
				
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.texcoord0;
				o.color = v.color;

				return o;
			}
			
			//Draw the circle
			fixed4 frag(fragIN i) : COLOR{
				fixed4 c = i.color;
				c.a = i.uv.x;

				return c;
			}
			
			ENDCG
			
		}
	} 
}
