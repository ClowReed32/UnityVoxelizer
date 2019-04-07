using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelizerMesh
{
	private ComputeBuffer positions;
	private ComputeBuffer normals;
	private ComputeBuffer uvs;
	private ComputeBuffer blendWeights;
	private ComputeBuffer blendIndex;

	public VoxelizerMesh(Mesh mesh)
	{
		positions = new ComputeBuffer(mesh.vertexCount, 3 * sizeof(float), ComputeBufferType.Default);
		positions.SetData(mesh.vertices);

		normals = new ComputeBuffer(mesh.normals.Length, 3 * sizeof(float), ComputeBufferType.Default);
		normals.SetData(mesh.normals);

		uvs = new ComputeBuffer(mesh.uv.Length, 2 * sizeof(float), ComputeBufferType.Default);
		uvs.SetData(mesh.uv);

		if(mesh.boneWeights.Length > 0)
		{
			float[] boneWeights = new float[mesh.boneWeights.Length*4];
			int[] boneIndexes = new int[mesh.boneWeights.Length*4];

			for(int i = 0; i < mesh.boneWeights.Length; i++)
			{
				boneWeights[4 * i] = mesh.boneWeights[i].weight0;
				boneWeights[4 * i + 1] = mesh.boneWeights[i].weight1;
				boneWeights[4 * i + 2] = mesh.boneWeights[i].weight2;
				boneWeights[4 * i + 3] = mesh.boneWeights[i].weight3;

				boneIndexes[4 * i] = mesh.boneWeights[i].boneIndex0;
				boneIndexes[4 * i + 1] = mesh.boneWeights[i].boneIndex1;
				boneIndexes[4 * i + 2] = mesh.boneWeights[i].boneIndex2;
				boneIndexes[4 * i + 3] = mesh.boneWeights[i].boneIndex3;
			}

			blendWeights = new ComputeBuffer(mesh.boneWeights.Length, 4 * sizeof(float), ComputeBufferType.Default);
			blendWeights.SetData(boneWeights);

			blendIndex = new ComputeBuffer(mesh.boneWeights.Length, 4 * sizeof(int), ComputeBufferType.Default);
			blendIndex.SetData(boneIndexes);
		}		
	}
}
