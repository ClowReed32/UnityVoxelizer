﻿using System.Collections;
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

	public void Render(Material mat)
	{
		mat.SetMatrix("_objectToWorld", gameObject.transform.localToWorldMatrix);

		foreach (var material in materials)
		{
			mat.SetVector("_albedo", material.GetColor("_Color"));
			mat.SetFloat("_metallic", material.GetFloat("_Metallic"));
			mat.SetFloat("_smoothness", material.GetFloat("_Glossiness"));

			Texture mainTex = material.GetTexture("_MainTex");
			Texture metallicGlossMap = material.GetTexture("_MetallicGlossMap");

			mat.SetInt("_useAlbedoMap", mainTex != null ? 1 : 0);
			mat.SetInt("_useMetallicGlossMap", metallicGlossMap != null ? 1 : 0);

			if (mainTex)
			{
				mainTex.filterMode = FilterMode.Point;
				mat.SetTexture("_mainAlbedo", mainTex);
			}

			if (metallicGlossMap)
			{
				metallicGlossMap.filterMode = FilterMode.Point;
				mat.SetTexture("_metallicGlossMap", metallicGlossMap);
			}

			mesh.Render(mat);
		}
	}
}