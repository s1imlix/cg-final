Shader "Custom/MarchingCubeMesh" {
    // Shader assigned to material and material is assigned to meshes, 
    // here we define how the meshes should be rendered
    Properties {
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200
        
        Pass {
            CGPROGRAM 
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct Vertex {
                float3 pos;
                float3 normal;
            };
            
            struct Triangle {
                Vertex vertexA;
                Vertex vertexB; 
                Vertex vertexC;
            };
            
            StructuredBuffer<Triangle> VertexBuffer; // Changed to Triangle buffer
            float4 _Color; // Color property from the shader
            
            struct appdata
            {
                uint vertexID : SV_VertexID;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 normal : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                uint triangleIndex = v.vertexID / 3;
                uint vertexIndex = v.vertexID % 3;
                
                Triangle tri = VertexBuffer[triangleIndex];
                Vertex vertex;
                
                if (vertexIndex == 0) vertex = tri.vertexA;
                else if (vertexIndex == 1) vertex = tri.vertexB;
                else vertex = tri.vertexC;
                
                o.pos = UnityObjectToClipPos(float4(vertex.pos, 1.0));
                o.normal = vertex.normal; 
                return o;
            }

            // https://docs.unity3d.com/cn/2021.1/Manual/SL-ShaderSemantics.html
            fixed4 frag(v2f i) : SV_Target
            {
                // diffuse: interpolated normal i.normal
                return _Color * (dot(normalize(i.normal), _WorldSpaceLightPos0) * 0.5 + 0.5); 
            }   
            ENDCG
        } 
    }
    FallBack "Diffuse"
}