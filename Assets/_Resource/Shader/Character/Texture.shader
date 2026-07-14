
Shader "T1/Character/Unlit/Texture" {
	Properties {
		_Color ("Main Color", Color) = (1,1,1,1)
		_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
		_intensity ("Intensity", float ) = 1.0
	}

	SubShader {
	Tags {"Queue"="Geometry" "IgnoreProjector"="True" "RenderType"="Opaque"}
	Fog {Mode Off}
	Blend Srcalpha OneMinusSrcAlpha
	
	Pass {  
		cull off
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest
			
			#include "UnityCG.cginc"

			struct appdata_t {
				float4 vertex : POSITION;
				half2 texcoord : TEXCOORD0;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				half2 texcoord : TEXCOORD0;
			};

			sampler2D _MainTex;
			fixed4 _Color;
			fixed _intensity;
			uniform fixed _gModelMipmapBias;
			
			v2f vert (appdata_t v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos( v.vertex);
				o.texcoord = v.texcoord;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2Dbias(_MainTex, float4(i.texcoord.xy, 0, _gModelMipmapBias)) * _Color;
				col.rgb *= _intensity;
				return col;
			}
		ENDCG
		}
	}
}
