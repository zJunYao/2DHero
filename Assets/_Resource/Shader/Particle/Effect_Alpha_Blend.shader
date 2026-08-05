// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'


// by artsyli

Shader "T1/Particles/Effect_AlphaBlend" 
{
	Properties 
	{
		_Color ("Main Color", Color) = (1, 1, 1, 1)
		_Brightness ("Brightness", Float) = 1.0
        _MainTex ("Base (RGB)", 2D) = "white" {}
		_UMainTexSpeed ("U MainTex Speed", float) = 0
		_VMainTexSpeed ("V MainTex Speed", float) = 0
        _UVParam ("UV Param", Vector) = (0, 0, 1, 1)
        //_UVTile ("UV Tile", Vector) = (0, 0, 1, 1)
        _Rotate ("UV Rotate", Range (0, 360.0)) = 0.0	
		[Toggle] Mask ("Mask", Float) = 0
		_MaskTex ("Mask Texture (R)", 2D) = "white" {}
        _MaskUVParam ("Mask UV Param", Vector) = (0, 0, 1, 1)
        _MaskRotate ("Mask UV Rotate", Range (0, 360.0)) = 0.0
		_UMaskFlowTexSpeed ("U MaskTex Speed", float) = 0
		_VMaskFlowTexSpeed ("V MaskTex Speed", float) = 0
		[Toggle] _Flow ("_Flow", Float) = 0
		_FlowUVParam ("_Flow UV Param", Vector) = (0, 0, 0, 1)
		_UFlowTexSpeed ("U FlowTex Speed", float) = 1
		_VFlowTexSpeed ("V FlowTex Speed", float) = 1
		_FlowTex ("Flow Texture (RG)", 2D) = "black" {}
		_FlowRotate ("FlowRotate", Range (0, 360.0)) = 0.0
		_DarkenDegree("***Do not edit***", Float) = 1
		_AlphaCtrl("Alpha control ***Do not edit***", Float) = 1
		_LuminosityAmount ("GrayScale Amount", Range(0.0, 1.0)) = 0.0
		[Toggle] _Dissolve ("_Dissolve", Float) = 0
		_DissolveTex ("Dissolve Tex", 2D) = "white" {}
        //_EdgeSoft ("Edge Soft", Range(-1, 2)) = 0.3050545
        //_DissolveScale ("DissolveScale", Float ) = 10
        _DissolveSub ("DissolveSub", Float ) = 0.5
		_DissolveBlend		("DissolveBlend", range(0,1)) = 0
	}
	SubShader 
	{
		Tags 
		{ 
			"Queue"="Transparent"
			"IgnoreProjector"="True"
			"RenderType"="Transparent" 
		}
		Pass
		{
			Blend Srcalpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Cull Off
			Lighting Off
	        ZWrite Off
//	        ZTest Off
	        Fog { Color (0,0,0,0) }
			Name "Effect"
			CGPROGRAM
		 
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile MASK_OFF MASK_ON
			#pragma multi_compile _FLOW_OFF  _FLOW_ON
			#pragma multi_compile _DISSOLVE_OFF  _DISSOLVE_ON
			#pragma target 2.0
			#include "UnityCG.cginc"
			
			sampler2D _MainTex;
			half _UMainTexSpeed;
			half _VMainTexSpeed;

			fixed4 _Color;
			half _Brightness;
			half4 _UVParam;
			//float4 _UVTile;
			half _Rotate;
			#ifdef MASK_ON
			sampler2D _MaskTex;
			half4 _MaskUVParam;
			half _MaskRotate;
			half _UMaskFlowTexSpeed;
			half _VMaskFlowTexSpeed;
			#endif

			#ifdef _FLOW_ON
			half4 _FlowUVParam;
			half _UFlowTexSpeed;
			half _VFlowTexSpeed;
			sampler2D _FlowTex;
			half _FlowRotate;
			#endif

			fixed _DarkenDegree;
			fixed _AlphaCtrl;
			
			float4 _MainTex_ST;  // build in var
			fixed _LuminosityAmount;
			
			#ifdef _DISSOLVE_ON
			sampler2D _DissolveTex;
			float4 _DissolveTex_ST;
            //float _EdgeSoft;
            //float _DissolveScale;
            half _DissolveSub;
			half _DissolveBlend;
            #endif

			float2 CalcUV(float2 uv, float4 uvParam, float4 uvTile, float uvRotate)
			{
				float2 outUV;
				float s;
				float c;
				s = sin(uvRotate);
				c = cos(uvRotate);
				
				outUV = uv - float2(0.5f, 0.5f);
				outUV = float2(outUV.x * c - outUV.y * s, outUV.x * s + outUV.y * c);
				outUV.x *= uvParam.z;
				outUV.y *= uvParam.w;
				outUV = outUV + uvParam.xy + float2(0.5f, 0.5f);
				
				//outUV.x = (outUV.x * uvTile.z) + uvTile.x;
				//outUV.y = (outUV.y * uvTile.w) + uvTile.y;
				
				return outUV;
			}

			float2 FlowRotate(float2 uv,float uvRotate)
			{
				float2 outUV;
				float s;
				float c;
				s = sin(uvRotate);
				c = cos(uvRotate);
				
				outUV = uv - float2(0.5f, 0.5f);
				outUV = float2(outUV.x * c - outUV.y * s, outUV.x * s + outUV.y * c);
				outUV = outUV + float2(0.5f, 0.5f);
				return outUV;
			}
			
			struct VS_INPUT
			{
				float4 position : POSITION;
				fixed4 color : COLOR;
				float2 uv : TEXCOORD0;
			};

			struct VS_OUTPUT
			{
				float4 position : SV_POSITION;
				fixed4 color : COLOR;
				float4 uv : TEXCOORD0;
				#ifdef _FLOW_ON  
				float2 flowUV : TEXCOORD1;
				#endif
				#ifdef _DISSOLVE_ON  
				float2 dissolveUV : TEXCOORD2;
				#endif
			};

			VS_OUTPUT vert(VS_INPUT In)
			{
				VS_OUTPUT Out;
				Out.position = UnityObjectToClipPos(In.position);

				float2 uv = In.uv.xy * _MainTex_ST.xy + _MainTex_ST.zw;
				Out.uv.xy = CalcUV(uv, _UVParam, float4(0, 0, 1, 1), _Rotate/57.2957796);
				
				#ifdef _DISSOLVE_ON
				Out.dissolveUV.xy = In.uv.xy * _DissolveTex_ST.xy + _DissolveTex_ST.zw;
				#endif

				#ifdef MASK_ON
					Out.uv.zw = CalcUV(In.uv, _MaskUVParam, float4(0, 0, 1, 1), _MaskRotate/57.2957796);
					Out.uv.z += _Time.y * _UMaskFlowTexSpeed;
					Out.uv.w += _Time.y * _VMaskFlowTexSpeed;
				#else
					Out.uv.zw = In.uv;
				#endif

				#ifdef _FLOW_ON			
					Out.flowUV.xy=FlowRotate(In.uv,_FlowRotate/57.2957796);
					Out.flowUV.xy = Out.flowUV.xy + _FlowUVParam.xy;
					Out.flowUV.x += _Time.y * _UFlowTexSpeed;
					Out.flowUV.y += _Time.y * _VFlowTexSpeed;
				#endif

				Out.uv.x += _Time.y * _UMainTexSpeed;
				Out.uv.y += _Time.y * _VMainTexSpeed;

				In.color.a *= _AlphaCtrl;
				Out.color = In.color;
				
				return Out;
			}
			
			float4 frag(VS_OUTPUT In) : COLOR 
			{
				#ifdef _FLOW_ON
					fixed4 flow = tex2D(_FlowTex, In.flowUV);
					flow += _FlowUVParam.w;
					float2 flow_uv = In.uv.xy + flow.rg *_FlowUVParam.z;
					fixed4 color = tex2D(_MainTex, flow_uv);
					#ifdef _DISSOLVE_ON
						float4 _DissolveTex_var = tex2D(_DissolveTex, flow_uv);
					#endif
				#else
					fixed4 color = tex2D(_MainTex, In.uv.xy);
					#ifdef _DISSOLVE_ON
						float4 _DissolveTex_var = tex2D(_DissolveTex, In.dissolveUV.xy);
					#endif
				#endif
				
				float luminosity = 0.299 * color.r + 0.587 * color.g + 0.114 * color.b;  
				color = lerp(color, luminosity, _LuminosityAmount);

				#ifdef _DISSOLVE_ON
	                //float dissolve_r = _DissolveTex_var.r * (In.color.a * _DissolveScale) - _DissolveSub;
					//color.a *= saturate(dissolve_r / _EdgeSoft);
					//In.color.a = 1;

					float t = max(_DissolveSub,(1 - In.color.a));
					fixed2 dissolveValues;
					dissolveValues.x = smoothstep( t,t + 0.05,_DissolveTex_var.r  * In.color.a * color.a);
					dissolveValues.y = (_DissolveTex_var.r - _DissolveSub - (1 - In.color.a)) * In.color.a;
					color.a *= lerp(dissolveValues.x,dissolveValues.y,_DissolveBlend);
				#endif

				color *= In.color;
				#ifdef MASK_ON
					color.a *= tex2D(_MaskTex, In.uv.zw).r;
				#endif

				color.rgba *= _Color.rgba;
				color.rgb *= _Brightness * _DarkenDegree;

				return color;
			}

			ENDCG
		}
	}
	Fallback off
	CustomEditor "EffectShaderGUI"
}
