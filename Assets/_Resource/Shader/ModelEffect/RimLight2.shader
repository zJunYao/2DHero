Shader "T1/ModelEffect/RimLight2" {
    Properties {
        _MainTex ("Tex", 2D) = "white" { }
        _Color ("Color", Color) = (1,0.7255578,0.5147059,1)
        _RimLevel ("RimLevel", Range(0, 10)) = 2.313089
        _RimPower ("RimPower", Range(0, 10)) = 2.478382
    }
    SubShader {
        Tags {
            "IgnoreProjector"="True"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        Pass {
            Tags {
                "LightMode"="ForwardBase"
            }
            Blend One One
            ZWrite On
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #pragma target 3.0
            uniform float4 _Color;
            uniform float _RimLevel;
            uniform float _RimPower;
            struct VertexInput {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 vertexColor : COLOR;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float4 posWorld : TEXCOORD0;
                float3 normalDir : TEXCOORD1;
                float4 vertexColor : COLOR;
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.vertexColor = v.vertexColor;
                o.normalDir = UnityObjectToWorldNormal(v.normal);
                o.posWorld = mul(unity_ObjectToWorld, v.vertex);
                o.pos = UnityObjectToClipPos( v.vertex );
                return o;
            }
            float4 frag(VertexOutput i) : COLOR {
                i.normalDir = normalize(i.normalDir);
                float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz);
                float3 normalDirection = i.normalDir;

                fixed rim = (1.0 - max(0, dot(i.normalDir, viewDirection)));
                float rimColor = (_Color.a * _RimPower * pow(rim, _RimLevel));
                float3 emissive = ((_Color.rgb * rimColor) * i.vertexColor.rgb);
                return fixed4(emissive, 1);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
    CustomEditor "ShaderForgeMaterialInspector"
}
