using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TextureDebugger : MonoBehaviour
{
	ComputeShader compute;
	int kernelIndex;

	RenderTexture outputTexture;
	Image outputImage;

	int slice = 128;

	// Start is called before the first frame update
	void Start()
    {
		outputTexture = new RenderTexture(256, 256, 32);
		outputTexture.enableRandomWrite = true;
		outputTexture.Create();
		outputTexture.filterMode = FilterMode.Point;

		outputImage = GameObject.Find("Canvas/OutputTextureDebugger").GetComponent<UnityEngine.UI.Image>();
		outputImage.material.mainTexture = outputTexture;
		outputImage.type = UnityEngine.UI.Image.Type.Simple;

		compute = Resources.Load<ComputeShader>("Shaders/Compute/TextureDebugger");
		kernelIndex = compute.FindKernel("CSMain");

		compute.SetTexture(kernelIndex, "output", outputTexture);
	}

    // Update is called once per frame
 //   void LateUpdate()
 //   {
	//	if (Input.GetKeyDown(KeyCode.Z))
	//	{
	//		slice = Mathf.Max(0, --slice);
	//		Debug.Log(slice + "\n");
	//	}
	//	if (Input.GetKeyDown(KeyCode.X))
	//	{
	//		slice = Mathf.Min(++slice, 255);
	//		Debug.Log(slice + "\n");
	//	}

	//	var texture = GameObject.Find("Voxelizer").GetComponent<Voxelizer>().voxelizedAlbedo;
	//	compute.SetTexture(kernelIndex, "input", texture);
	//	compute.SetInt("slice", slice);

	//	uint x, y, z;
	//	compute.GetKernelThreadGroupSizes(kernelIndex, out x, out y, out z);
	//	compute.Dispatch(kernelIndex, outputTexture.width / (int)x, outputTexture.height / (int)y, 1);
	//}
}
