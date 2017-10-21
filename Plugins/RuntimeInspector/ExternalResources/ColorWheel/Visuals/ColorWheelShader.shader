// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

//Copyright (c) 2015, Felix Kate All rights reserved.
// Usage of this code is governed by a BSD-style license that can be found in the LICENSE file.

Shader "UI/ColorWheel" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		//_BorderWhiteness ("Border Whiteness", Range(0.0,1.0)) = 1.0
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
			};
			
			struct fragIN{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
			};
			
			//Function for making smooth circles from gradient
			fixed smoothCircle(fixed size, fixed gradient){
				fixed scaleFactor = size + 1;
				return smoothstep(0.5 - 0.0025 * scaleFactor, 0.5 + 0.0025 * scaleFactor, 1 - gradient * scaleFactor);
			}
			
			//Function for making box from gradient
			fixed smoothBox(fixed size, fixed2 gradient){
				fixed scaleFactor = size * 0.5;
				fixed alpha = ceil(gradient.x - scaleFactor);
				alpha *= ceil((1 - gradient.x) - scaleFactor);
				alpha *= ceil(gradient.y - scaleFactor);
				alpha *= ceil((1 - gradient.y) - scaleFactor);
				
				return alpha;
			}
			
			//Get the values from outside
			fixed4 _Color;
			//fixed _BorderWhiteness;

			//Fill the vert struct
			fragIN vert (vertIN v){
				fragIN o;
				
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.texcoord0;
				
				return o;
			}
			
			//Draw the circle
			fixed4 frag(fragIN i) : COLOR{
				fixed4 c = 1;// _BorderWhiteness1;
				
				//Make the inner area of the box
				fixed2 bGrad = 1;
				bGrad.x = smoothstep(0.25, 0.75, i.uv.x);
				bGrad.y = smoothstep(0.25, 0.75, i.uv.y);
				
				fixed4 cBox = lerp(1, _Color, bGrad.x) * bGrad.y;
				
				//Set up PI
				fixed PI = 3.14159265359;

				//Circular gradient
				fixed cGrad = distance(i.uv, fixed2(0.5, 0.5));
				
				//Angle gradient
				fixed aGrad = (atan2(1 - i.uv.x - 0.5, 1 - i.uv.y - 0.5) + PI) / (2 * PI);
				fixed ang = aGrad * PI * 2;

				//Calculate hue
				fixed4 cWheel = 1;
				
				cWheel.r = clamp(2/PI * asin(cos(ang)) * 1.5 + 0.5, 0, 1);
				cWheel.g = clamp(2/PI * asin(cos(2 * PI * (1.0/3.0) - ang)) * 1.5 + 0.5, 0, 1);
				cWheel.b = clamp(2/PI * asin(cos(2 * PI * (2.0/3.0) - ang)) *  1.5 + 0.5, 0, 1);

								
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
