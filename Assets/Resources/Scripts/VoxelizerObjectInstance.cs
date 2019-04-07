using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelizerObjectInstance
{
	private GameObject gameObject;
	private VoxelizerMesh mesh;
	private Material[] materials;

	public VoxelizerObjectInstance(GameObject gameObject)
	{
		this.gameObject = gameObject;
		this.materials = gameObject.GetComponent<Renderer>().materials;

		var skinnedMesh = gameObject.GetComponent<SkinnedMeshRenderer>();
		var mesh = gameObject.GetComponent<MeshFilter>();

		if(mesh)
		{
			this.mesh = VoxelizerMeshLibray.getMesh(mesh.sharedMesh);
		}
		else if(skinnedMesh)
		{
			this.mesh = VoxelizerMeshLibray.getMesh(skinnedMesh.sharedMesh);
		}
		else
		{
			Debug.LogError("Unknown mesh type.");
		}
	}
}
