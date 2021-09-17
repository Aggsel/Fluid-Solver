//Shader is originally based on:
//https://www.ronja-tutorials.com/post/051-draw-procedural/

Shader "Unlit/ParticleShader"
{
    Properties{
        [HDR] _Color ("Tint", Color) = (0, 0, 0, 1)
        _LightDirection ("Light Direction", Vector) = (0, 0.707106, 0.707106)
    }
    SubShader{
        Tags{ "RenderType"="Opaque" "Queue"="Geometry" }

        Pass{
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            //tint of the texture
            fixed4 _Color;
            float3 _LightDirection;

            struct particle{
                float x;
                float y;
                float z;
                float dx;
                float dy;
                float dz;
                float dummy;
                float dummy2;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
            };

            //buffers
            StructuredBuffer<particle> particles;
            StructuredBuffer<int> triangles;
            StructuredBuffer<float3> vertices;
            StructuredBuffer<float3> normals;

            //the vertex shader function
            v2f vert(uint vertex_id: SV_VertexID, uint instance_id: SV_InstanceID){
                v2f o;
                int positionIndex = triangles[vertex_id];
                float3 position = vertices[positionIndex];
                //add sphere position
                position += float3(particles[instance_id].x, particles[instance_id].y, particles[instance_id].z);
                //convert the vertex position from world space to clip space
                o.pos = mul(UNITY_MATRIX_VP, float4(position, 1));
                o.color = float4(normals[positionIndex], 1);
                return o;
            }

            //the fragment shader function
            fixed4 frag(v2f i) : SV_TARGET{
                //return the final color to be drawn on screen
                float4 finalColor = dot(normalize(i.color), normalize(_LightDirection)) * _Color;
                return finalColor;
            }

            ENDCG
            }   
        }
        Fallback "VertexLit"
}
