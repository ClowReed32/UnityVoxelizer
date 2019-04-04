Shader "Unlit/DrawVoxels"
{
    Properties
    {
    }
    SubShader
    {
		Pass
        {
			Tags {"LightMode" = "Deferred"}
			LOD 100

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#pragma exclude_renderers nomrt
			#pragma target 5.0
			
			#define LIGHTPROBE_SH 1
			#define UNITY_HDR_ON 1

			#include "UnityStandardConfig.cginc"
            #include "UnityCG.cginc"
			#include "UnityPBSLighting.cginc"

			struct VsIn
			{
				float3 vertexPosition_modelspace : Position;
				float3 vertexNormal_modelspace	 : Normal;
				uint InstanceId : SV_InstanceID;
			};

			struct PsIn
			{
				float4 vertexDepth_projectspace  : SV_Position;
				float3 vertexNormal_worldspace	 : Normal;
				float3 vertexPosition_worldspace : TexCoord0;
				uint3 voxelPosition				 : TexCoord1;
			};

			struct PsOut
			{
				half4 albedo : SV_Target0;
				half4 specular : SV_Target1;
				half4 normal : SV_Target2;
				half4 emission : SV_Target3;
			};

			struct FragmentCommonData
			{
				half3 posWorld;
				half smoothness;
				half3 eyeVec;
				half3 normalWorld;
				half3 specColor;
			};

			StructuredBuffer< uint3 > voxelPositions;
			Texture3D<int> voxelizedAlbedo;
			Texture3D<int> voxelizedMetallicSmothness;

			float voxelSize;
			float4 gridOffset;

			float4 cameraPosition;

			PsIn vert (VsIn input)
            {
				PsIn output;

				uint3 voxelPos = voxelPositions[input.InstanceId];
				float3 vertexPos = voxelSize*input.vertexPosition_modelspace - gridOffset.xyz + 0.5f*voxelSize;
				vertexPos += float3(voxelPos) * float3(voxelSize, voxelSize, voxelSize);

				output.vertexPosition_worldspace = vertexPos;
				output.vertexDepth_projectspace = mul(UNITY_MATRIX_VP, float4(vertexPos, 1.0f));
				output.vertexNormal_worldspace = input.vertexNormal_modelspace;
				output.voxelPosition = voxelPos;

                return output;
            }

			float4 convRGBA8Tofloat4(uint val)
			{
				return float4(float((val & 0x000000FF)), float((val & 0x0000FF00) >> 8U), float((val & 0x00FF0000) >> 16U), float((val & 0xFF000000) >> 24U)) / 255.0f;
			}

			inline UnityGI FragmentGI(FragmentCommonData s, half occlusion, half4 i_ambientOrLightmapUV, half atten, UnityLight light, bool reflections)
			{
				UnityGIInput d;
				d.light = light;
				d.worldPos = s.posWorld;
				d.worldViewDir = -s.eyeVec;
				d.atten = atten;
				#if defined(LIGHTMAP_ON) || defined(DYNAMICLIGHTMAP_ON)
					d.ambient = 0;
					d.lightmapUV = i_ambientOrLightmapUV;
				#else
					d.ambient = i_ambientOrLightmapUV.rgb;
					d.lightmapUV = 0;
				#endif

				d.probeHDR[0] = unity_SpecCube0_HDR;
				d.probeHDR[1] = unity_SpecCube1_HDR;
				#if defined(UNITY_SPECCUBE_BLENDING) || defined(UNITY_SPECCUBE_BOX_PROJECTION)
					d.boxMin[0] = unity_SpecCube0_BoxMin; // .w holds lerp value for blending
				#endif
				#ifdef UNITY_SPECCUBE_BOX_PROJECTION
					d.boxMax[0] = unity_SpecCube0_BoxMax;
					d.probePosition[0] = unity_SpecCube0_ProbePosition;
					d.boxMax[1] = unity_SpecCube1_BoxMax;
					d.boxMin[1] = unity_SpecCube1_BoxMin;
					d.probePosition[1] = unity_SpecCube1_ProbePosition;
				#endif

				if (reflections)
				{
					Unity_GlossyEnvironmentData g = UnityGlossyEnvironmentSetup(s.smoothness, -s.eyeVec, s.normalWorld, s.specColor);
					// Replace the reflUVW if it has been compute in Vertex shader. Note: the compiler will optimize the calcul in UnityGlossyEnvironmentSetup itself
					/*#if UNITY_STANDARD_SIMPLE
						g.reflUVW = s.reflUVW;
					#endif*/

					return UnityGlobalIllumination(d, occlusion, s.normalWorld, g);
				}
				else
				{
					return UnityGlobalIllumination(d, occlusion, s.normalWorld);
				}
			}

			PsOut frag (PsIn input)
            {
				PsOut output;

				float4 albedo = convRGBA8Tofloat4(uint(voxelizedAlbedo[input.voxelPosition]));
				float4 metallicSmoothness = convRGBA8Tofloat4(uint(voxelizedMetallicSmothness[input.voxelPosition]));

				float3 specularColor;
				float oneMinusReflectivity;
				float3 diffuseColor = DiffuseAndSpecularFromMetallic(albedo.rgb, metallicSmoothness.r, specularColor, oneMinusReflectivity);
				
				UnityLight dummyLight;
				dummyLight.color = 0;
				dummyLight.dir = half3 (0, 1, 0);

				half3 eyeVec = normalize(cameraPosition.xyz - input.vertexPosition_worldspace);

				FragmentCommonData s;
				s.posWorld = input.vertexPosition_worldspace;
				s.smoothness = metallicSmoothness.g;
				s.eyeVec = eyeVec;
				s.normalWorld = input.vertexNormal_worldspace;
				s.specColor = specularColor;

				UnityGI gi = FragmentGI(s, 1.0f, 0.0f, 1.0f, dummyLight, true);

				half3 emissiveColor = UNITY_BRDF_PBS(diffuseColor, specularColor, oneMinusReflectivity, metallicSmoothness.g, input.vertexNormal_worldspace, -eyeVec, gi.light, gi.indirect).rgb;

				#ifndef UNITY_HDR_ON
					emissiveColor.rgb = exp2(-emissiveColor.rgb);
				#endif

				output.albedo = float4(diffuseColor, 1.0f);
				output.specular = float4(specularColor, metallicSmoothness.g);
				output.normal = float4(input.vertexNormal_worldspace*0.5f + 0.5f, 1.0f);
				output.emission = float4(emissiveColor, 1.0f);

				return output;
            }
            ENDCG
        }

		Pass
		{
			Tags {"LightMode" = "ShadowCaster"}

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_shadowcaster
			#include "UnityCG.cginc"

			struct VsIn
			{
				float3 vertexPosition_modelspace : Position;
				float3 vertexNormal_modelspace	 : Normal;
				uint InstanceId : SV_InstanceID;
			};

			struct PsIn
			{
				float4 pos : SV_Position;
			};

			StructuredBuffer < uint3 > voxelPositions;
			float voxelSize;
			float4 gridOffset;

			PsIn vert(VsIn input)
			{
				PsIn output;

				uint3 voxelPos = voxelPositions[input.InstanceId];
				float3 vertexPos = voxelSize * input.vertexPosition_modelspace - gridOffset.xyz + 0.5f*voxelSize;
				vertexPos += float3(voxelPos) * float3(voxelSize, voxelSize, voxelSize);

				output.pos = mul(UNITY_MATRIX_VP, float4(vertexPos, 1.0));
				output.pos.z += unity_LightShadowBias.x;
				return output;
			}

			float4 frag(PsIn input) : SV_Target
			{
				return float4(0.0f, 0.0f, 0.0f, 0.0f);
			}
			ENDCG
		}
    }
}
