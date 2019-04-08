using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelizerMesh
{
	private ComputeBuffer positions = null;
	private ComputeBuffer normals = null;
	private ComputeBuffer uvs = null;
	private ComputeBuffer blendWeights = null;
	private ComputeBuffer blendIndexes = null;
	private ComputeBuffer triangles = null;

	public VoxelizerMesh(Mesh mesh)
	{
		positions = new ComputeBuffer(mesh.vertexCount, 3 * sizeof(float), ComputeBufferType.Default);
		positions.SetData(mesh.vertices);

		normals = new ComputeBuffer(mesh.normals.Length, 3 * sizeof(float), ComputeBufferType.Default);
		normals.SetData(mesh.normals);

		if(mesh.uv.Length > 0)
		{
			uvs = new ComputeBuffer(mesh.uv.Length, 2 * sizeof(float), ComputeBufferType.Default);
			uvs.SetData(mesh.uv);
		}		

		if(mesh.boneWeights.Length > 0)
		{
			float[] boneWeights = new float[mesh.boneWeights.Length*4];
			int[] boneIndexes = new int[mesh.boneWeights.Length*4];

			int i = 0;

			foreach(var bone in mesh.boneWeights)
			{
				boneWeights[4 * i] = bone.weight0;
				boneWeights[4 * i + 1] = bone.weight1;
				boneWeights[4 * i + 2] = bone.weight2;
				boneWeights[4 * i + 3] = bone.weight3;

				boneIndexes[4 * i] = bone.boneIndex0;
				boneIndexes[4 * i + 1] = bone.boneIndex1;
				boneIndexes[4 * i + 2] = bone.boneIndex2;
				boneIndexes[4 * i + 3] = bone.boneIndex3;

				i++;
			}

			blendWeights = new ComputeBuffer(mesh.boneWeights.Length, 4 * sizeof(float), ComputeBufferType.Default);
			blendWeights.SetData(boneWeights);

			blendIndexes = new ComputeBuffer(mesh.boneWeights.Length, 4 * sizeof(int), ComputeBufferType.Default);
			blendIndexes.SetData(boneIndexes);
		}

		triangles = new ComputeBuffer(mesh.triangles.Length, sizeof(int), ComputeBufferType.Default);
		triangles.SetData(mesh.triangles);
	}

	public void Render(Material mat)
	{
		mat.SetBuffer("_positions", positions);
		mat.SetBuffer("_normals", normals);

		mat.SetInt("_useUvs", uvs != null ? 1 : 0);

		if(uvs != null)
		{
			mat.SetBuffer("_uvs", uvs);
		}	

		mat.SetBuffer("_triangles", positions);

		mat.SetPass(0);

		Graphics.DrawProcedural(MeshTopology.Triangles, triangles.count);
	}

	public void Release()
	{
		if (positions != null)
			positions.Release();
		positions = null;

		if (normals != null)
			normals.Release();
		normals = null;

		if (uvs != null)
			uvs.Release();
		uvs = null;

		if (blendWeights != null)
			blendWeights.Release();
		blendWeights = null;

		if (blendIndexes != null)
			blendIndexes.Release();
		blendIndexes = null;

		if (triangles != null)
			triangles.Release();
		triangles = null;
	}
}
