Shader "Custom/MarchingCubeMesh" {
    // Shader assigned to material and material is assigned to meshes, 
    // here we define how the meshes should be rendered
    Properties {

    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200
        
        CGPROGRAM 
        #pragma vertex vert
        #pragma fragment frag
        #include "UnityCG.cginc"
        
        struct Vertex {
            float3 pos;
            float3 normal;
        };
        StructuredBuffer<Vertex> VertexBuffer; // Buffer containing vertices of the mesh
        float4 color;

        
        ENDCG
    }
    FallBack "Diffuse"
}