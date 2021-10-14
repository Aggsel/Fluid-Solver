//Shader is originally based on:
//https://www.ronja-tutorials.com/post/051-draw-procedural/

Shader "Unlit/ParticleShader"
{
    Properties{
        [HDR] _Color ("Tint", Color) = (0, 0, 0, 1)
        _LightDirection ("Light Direction", Vector) = (0, 0.707106, 0.707106)
        _ParticleScale ("Particle Scale", float) = 1
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
            float _ParticleScale;

            struct particle{
                float3 position;
                float density;
                float pressure;
                float3 forces;
                float3 velocity;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
                fixed3 normal : NORMAL;
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
                float3 position = vertices[positionIndex] * _ParticleScale;
                //add sphere position
                position += particles[instance_id].position;
                //convert the vertex position from world space to clip space
                o.pos = mul(UNITY_MATRIX_VP, float4(position, 1));
                o.normal = float4(normals[positionIndex], 1);

                float3 velocity = particles[instance_id].velocity;
                float3 speed = float3(abs(velocity.x), abs(velocity.y), abs(velocity.z)) / 10;
                o.color = float4(speed, 1.0f) + float4(0.1f, 0.1f, 0.1f, 1.0f);
                return o;
            }

            //the fragment shader function
            fixed4 frag(v2f i) : SV_TARGET{
                //return the final color to be drawn on screen
                // float4 finalColor = dot(normalize(i.normal), normalize(_LightDirection)) * i.color;
                return i.color;
            }

            ENDCG
            }   
        }
        Fallback "VertexLit"
}
