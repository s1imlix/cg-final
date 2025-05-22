Shader "Custom/ParticleSphere" {
    // Shader assigned to material and material is assigned to meshes, 
    // here we define how the meshes should be rendered
    Properties {
        _Color ("Color", Color) = (0,0,1,1)
        _GradientTex ("Gradient Texture", 2D) = "white" {}
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200
        
        CGPROGRAM // Start of the shader program
        /*
        https://docs.unity3d.com/Manual//SL-PragmaDirectives.html
        #pragma surface <surface function> <lighting model> 
        pragma surface surf -> defines surf() as the surface function
        surf(): (UV, ...) -> (Albedo, Normal, Specular, etc.)
        */
        #pragma surface surf Lambert
        #pragma vertex vert
        #pragma multi_compile_instancing

        /*
        GPU instancing: CPU does transform -> GPU rendering multiple meshes at once
        https://docs.unity3d.com/2022.3/Documentation/Manual/gpu-instancing-shader.html
        Procedual instancing: GPU does transform (computeBuffer) -> GPU rendering multiple meshes at once
        */
        #pragma instancing_options procedural:setup
        
        struct Input {
            float2 uv_MainTex;
            float3 color;
        };
        
        float4 _Color;
        float _ParticleRadius;
        int useMaterialColor;
        sampler2D _GradientTex;
        float Vmax;
        

        #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            struct Particle
            {
                float3 position;
                float3 velocity;
                float3 predictPosition;
                float density;
                float nearDensity;
            };
            StructuredBuffer<Particle> _ParticleBuffer;
        #endif
        
        void setup() {
            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                Particle particle = _ParticleBuffer[unity_InstanceID];
                
                // GPU transform the mesh to the position of the particle
                float scale = _ParticleRadius * 2;
                unity_ObjectToWorld._11_21_31_41 = float4(scale, 0, 0, 0);
                unity_ObjectToWorld._12_22_32_42 = float4(0, scale, 0, 0);
                unity_ObjectToWorld._13_23_33_43 = float4(0, 0, scale, 0);
                unity_ObjectToWorld._14_24_34_44 = float4(particle.position, 1); 

                
            #endif
        }

        void vert(inout appdata_full v, out Input o) {
            UNITY_INITIALIZE_OUTPUT(Input, o);
    
            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            float3 pv = _ParticleBuffer[unity_InstanceID].velocity;
            float speedNorm = saturate(length(pv) / Vmax);
            o.color = tex2Dlod(_GradientTex, float4(speedNorm, 0.5, 0, 0)); // sample color here
            #endif
        }

        void surf (Input IN, inout SurfaceOutput o) {
            o.Albedo = useMaterialColor == 1 ? _Color.rgb : IN.color.rgb;
            o.Alpha = 1.0;
        }
        ENDCG
    }
    FallBack "Diffuse"
}