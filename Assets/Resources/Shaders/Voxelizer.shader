﻿Shader "Voxelizer"
{
	Properties
	{
	}
		SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 100

		Pass
		{
			Cull Off
			ZWrite Off

			CGPROGRAM
			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag

			#pragma target 5.0

			#include "UnityCG.cginc"

			struct GsInput
			{
				float3 vertexNormal_worldspace   : Normal;
				float4 vertexPosition_unitcube   : SV_Position;
				float3 vertexPosition_worldspace : TexCoord0;
				float2 vertexUV					 : TexCoord1;
			};

			struct PsInput
			{
				float3 gs_VertexNormal_worldspace	: Normal;
				float4 gs_VertexPosition_unitspace	: SV_Position;
				float3 gs_VertexPosition_worldspace	: TexCoord0;
				float2 gs_VertexUV					: TexCoord1;
				float4 gs_VertexPosition_voxelspace : TexCoord2;
			};

			float4x4 _worldToUnitCube;
			float4x4 _objectToWorld;

			RWTexture3D<int> _voxelizedAlbedo : register(u1);
			RWTexture3D<int> _voxelizedMetallicSmoothness : register(u2);

			float4 _albedo;
			float _metallic;
			float _smoothness;

			int _useAlbedoMap;
			int _useMetallicGlossMap;
			Texture2D<float4> _mainAlbedo;
			Texture2D<float4> _metallicGlossMap;
			SamplerState sampler_mainAlbedo;
			SamplerState sampler_metallicGlossMap;

			StructuredBuffer<float3> _positions;
			StructuredBuffer<float3> _normals;
			StructuredBuffer<float2> _uvs;
			int _useUvs;
			StructuredBuffer<int> _triangles;

			GsInput vert(uint id : SV_VertexID)
			{
				GsInput output;

				int index = _triangles[id];
				float3 vertexPosition_modelspace = _positions[index];
				float3 vertexNormal_modelspace = _normals[index];
				float2 vertexUV_modelspace = _useUvs == 1 ? _uvs[index] : 0.0f;

				output.vertexPosition_worldspace = mul(_objectToWorld, float4(vertexPosition_modelspace, 1.0f));
				output.vertexPosition_unitcube = mul(_worldToUnitCube, float4(output.vertexPosition_worldspace, 1.0f));
				output.vertexNormal_worldspace = mul(unity_ObjectToWorld, float4(vertexNormal_modelspace, 0.0f));
				output.vertexUV = vertexUV_modelspace;
				return output;
			}

			[maxvertexcount(3)]
			void geom(triangle GsInput input[3], inout TriangleStream <PsInput> outputStream)
			{
				PsInput output;

				// Calculate swizzle matrix based on eye space normal's dominant direction.
				float3 unitSpaceV1 = normalize(input[1].vertexPosition_unitcube.xyz - input[0].vertexPosition_unitcube.xyz);
				float3 unitSpaceV2 = normalize(input[2].vertexPosition_unitcube.xyz - input[0].vertexPosition_unitcube.xyz);
				float3 unitSpaceNormal = abs(cross(unitSpaceV1, unitSpaceV2));
				float dominantAxis = max(unitSpaceNormal.x, max(unitSpaceNormal.y, unitSpaceNormal.z));
				float4x4 swizzleMatrix;

				if (dominantAxis == unitSpaceNormal.x)
				{
					swizzleMatrix = float4x4(float4(0.0, 0.0, 1.0, 0.0f),
						float4(0.0, 1.0, 0.0, 0.0f),
						float4(-1.0, 0.0, 0.0, 0.0f),
						float4(0.0, 0.0, 0.0, 1.0f));
				}
				else if (dominantAxis == unitSpaceNormal.y)
				{
					swizzleMatrix = float4x4(float4(1.0, 0.0, 0.0, 0.0f),
						float4(0.0, 0.0, -1.0, 0.0f),
						float4(0.0, 1.0, 0.0, 0.0f),
						float4(0.0, 0.0, 0.0, 1.0f));
				}
				else if (dominantAxis == unitSpaceNormal.z)
				{
					swizzleMatrix = float4x4(float4(1.0, 0.0, 0.0, 0.0f),
						float4(0.0, 1.0, 0.0, 0.0f),
						float4(0.0, 0.0, 1.0, 0.0f),
						float4(0.0, 0.0, 0.0, 1.0f));
				}

				float4x4 gridProjectionMatrix = float4x4(float4(1.0f, 0.0f, 0.0f, 0.0f),
														float4(0.0f, 1.0f, 0.0f, 0.0f),
														float4(0.0f, 0.0f, 0.5f, 0.0f),
														float4(0.0f, 0.0f, 0.5f, 1.0f));

				// Calculate screen coordinates for triangle.
				float4 screenPos[3];
				screenPos[0] = mul(mul(float4(input[0].vertexPosition_unitcube.xyz, 1.0), swizzleMatrix), gridProjectionMatrix);
				screenPos[1] = mul(mul(float4(input[1].vertexPosition_unitcube.xyz, 1.0), swizzleMatrix), gridProjectionMatrix);
				screenPos[2] = mul(mul(float4(input[2].vertexPosition_unitcube.xyz, 1.0), swizzleMatrix), gridProjectionMatrix);
				screenPos[0] /= screenPos[0].w;
				screenPos[1] /= screenPos[1].w;
				screenPos[2] /= screenPos[2].w;

				// Output triangle.
				output.gs_VertexPosition_worldspace = input[0].vertexPosition_worldspace;
				output.gs_VertexNormal_worldspace = input[0].vertexNormal_worldspace;
				output.gs_VertexPosition_unitspace = screenPos[0];
				output.gs_VertexUV = input[0].vertexUV;
				output.gs_VertexPosition_voxelspace = input[0].vertexPosition_unitcube;
				outputStream.Append(output);

				output.gs_VertexPosition_worldspace = input[1].vertexPosition_worldspace;
				output.gs_VertexNormal_worldspace = input[1].vertexNormal_worldspace;
				output.gs_VertexPosition_unitspace = screenPos[1];
				output.gs_VertexUV = input[1].vertexUV;
				output.gs_VertexPosition_voxelspace = input[1].vertexPosition_unitcube;
				outputStream.Append(output);

				output.gs_VertexPosition_worldspace = input[2].vertexPosition_worldspace;
				output.gs_VertexNormal_worldspace = input[2].vertexNormal_worldspace;
				output.gs_VertexPosition_unitspace = screenPos[2];
				output.gs_VertexUV = input[2].vertexUV;
				output.gs_VertexPosition_voxelspace = input[2].vertexPosition_unitcube;
				outputStream.Append(output);

				outputStream.RestartStrip();
			}

			uint convfloat4ToRGBA8(float4 val)
			{
				val *= 255.0f;
				return (uint(val.w) & 0x000000FF) << 24U | (uint(val.z) & 0x000000FF) << 16U | (uint(val.y) & 0x000000FF) << 8U | (uint(val.x) & 0x000000FF);
			}

			void imageAtomicRGBA8(RWTexture3D<int> tex, int3 coords, float4 newVal)
			{
				uint newValU = convfloat4ToRGBA8(newVal);
				uint lastValU = 0;
				uint currValU;

				InterlockedMax(tex[coords], newValU, currValU);
			}

			void frag(PsInput input)
			{
				int x, y, z;
				_voxelizedAlbedo.GetDimensions(x, y, z);

				float4 storagePos = input.gs_VertexPosition_voxelspace;
				storagePos.xyz *= 1.0 / storagePos.w;
				int3 voxelPos = int3(int3(x, y, z) * (storagePos.xyz * 0.5f + 0.5f));

				float3 albedo = _albedo.rgb;
				float smoothness = _smoothness;
				float metallic = _metallic;

				if (_useAlbedoMap)
				{
					albedo *= UNITY_SAMPLE_TEX2D(_mainAlbedo, input.gs_VertexUV);
				}

				if (_useMetallicGlossMap)
				{
					float4 sampleSM = UNITY_SAMPLE_TEX2D(_metallicGlossMap, input.gs_VertexUV);
					smoothness = sampleSM.a;
					metallic = sampleSM.r;
				}

				/*_voxelizedAlbedo[voxelPos] = int(convfloat4ToRGBA8(float4(albedo, 1.0f)));
				_voxelizedMetallicSmoothness[voxelPos] = int(convfloat4ToRGBA8(float4(metallic, smoothness, 0.0f, 1.0f)));*/

				imageAtomicRGBA8(_voxelizedAlbedo, voxelPos, float4(albedo, 1.0f));
				imageAtomicRGBA8(_voxelizedMetallicSmoothness, voxelPos, float4(metallic, smoothness, 0.0f, 1.0f));
			}
			ENDCG
		}
	}
}
