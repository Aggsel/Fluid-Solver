//Shader is originally based on:
//https://www.ronja-tutorials.com/post/051-draw-procedural/

Shader "Unlit/ParticleShader"
{
    Properties{
        [HDR] _Color ("Tint", Color) = (0, 0, 0, 1)
    }
    SubShader{
    //the material is completely non-transparent and is rendered at the same time as the other opaque geometry
    Tags{ "RenderType"="Opaque" "Queue"="Geometry" }

    Pass{
        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag

        #include "UnityCG.cginc"

        //tint of the texture
        fixed4 _Color;

        struct particle{
            float x;
            float y;
            float z;
            float dummy;
        };

        //buffers
        StructuredBuffer<particle> particles;
        StructuredBuffer<int> triangles;
        StructuredBuffer<float3> vertices;

        //the vertex shader function
        float4 vert(uint vertex_id: SV_VertexID, uint instance_id: SV_InstanceID) : SV_POSITION{
            //get vertex position
            int positionIndex = triangles[vertex_id];
            float3 position = vertices[positionIndex];
            //add sphere position
            position += float3(particles[instance_id].x, particles[instance_id].y, particles[instance_id].z);
            //convert the vertex position from world space to clip space
            return mul(UNITY_MATRIX_VP, float4(position, 1));
        }

        //the fragment shader function
        fixed4 frag() : SV_TARGET{
            //return the final color to be drawn on screen
            return _Color;
        }

        ENDCG
        }   
    }
    Fallback "VertexLit"
}
