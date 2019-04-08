using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelizerMeshLibray
{
	private static Dictionary<string, VoxelizerMesh> meshDictionary = new Dictionary<string, VoxelizerMesh>();

	public static VoxelizerMesh getMesh(Mesh nativeMesh)
	{
		VoxelizerMesh mesh = null;

		if(!meshDictionary.ContainsKey(nativeMesh.name))
		{
			mesh = new VoxelizerMesh(nativeMesh);
			meshDictionary[nativeMesh.name] = mesh;
		}
		else
		{
			mesh = meshDictionary[nativeMesh.name];
		}

		return mesh;
	}

	public static void release()
	{
		foreach(var mesh in meshDictionary)
		{
			mesh.Value.Release();
		}
	}
}
