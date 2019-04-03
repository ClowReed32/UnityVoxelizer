using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Voxelizer : MonoBehaviour
{
	public int size = 256;

	public RenderTexture voxelizedAlbedo;
	public RenderTexture voxelizedMetallicSmoothness;

	private ComputeShader occupiedVoxelCounter;
	private int occupiedVoxelCounterIndex;

	private ComputeShader clearCompute;
	private int clearKernelIndex;

	private ComputeBuffer positionBuffer;
	private ComputeBuffer argsBuffer;
	private uint[] args = new uint[5] { 36, 0, 0, 0, 0 };

	private Material drawVoxelMaterial;
	private Material voxelizerMaterial;
	private Matrix4x4 worldToUnitCube;
	private float voxelScale;
	private Vector4 voxelBias;

	private Bounds gridBounds;

	private Mesh cube;

	private List<GameObject> sceneObjects = new List<GameObject>();

	void computeWorldToUnitCubeMatrix()
	{
		this.gridBounds = GameObject.Find("VoxelizeGrid").GetComponent<Renderer>().bounds;

		this.voxelScale = 1.0f / Mathf.Max(Mathf.Max(this.gridBounds.size.x, this.gridBounds.size.y), this.gridBounds.size.z);
		this.voxelBias = -this.gridBounds.min;

		worldToUnitCube = Matrix4x4.Translate(new Vector3(-1.0f, -1.0f, -1.0f)) * Matrix4x4.Scale(new Vector3(2.0f, 2.0f, 2.0f)) * Matrix4x4.Scale(new Vector3(voxelScale, voxelScale, voxelScale)) * Matrix4x4.Translate(voxelBias);
	}

	void createVoxelBuffer()
	{
		voxelizedAlbedo = new RenderTexture(size, size, 0, RenderTextureFormat.ARGB32);
		voxelizedAlbedo.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
		voxelizedAlbedo.volumeDepth = size;
		voxelizedAlbedo.enableRandomWrite = true;
		voxelizedAlbedo.Create();
		voxelizedAlbedo.filterMode = FilterMode.Point;

		voxelizedMetallicSmoothness = new RenderTexture(size, size, 0, RenderTextureFormat.ARGB32);
		voxelizedMetallicSmoothness.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
		voxelizedMetallicSmoothness.volumeDepth = size;
		voxelizedMetallicSmoothness.enableRandomWrite = true;
		voxelizedMetallicSmoothness.Create();
		voxelizedMetallicSmoothness.filterMode = FilterMode.Point;
	}

	void createCubeMesh()
	{
		Vector3 p0 = new Vector3(-.5f, -.5f, .5f);
		Vector3 p1 = new Vector3(.5f, -.5f, .5f);
		Vector3 p2 = new Vector3(.5f, -.5f, -.5f);
		Vector3 p3 = new Vector3(-.5f, -.5f, -.5f);

		Vector3 p4 = new Vector3(-.5f, .5f, .5f);
		Vector3 p5 = new Vector3(.5f, .5f, .5f);
		Vector3 p6 = new Vector3(.5f, .5f, -.5f);
		Vector3 p7 = new Vector3(-.5f, .5f, -.5f);

		Vector3[] vertices = new Vector3[]
		{
			// Bottom
			p0, p1, p2, p3,
 
			// Left
			p7, p4, p0, p3,
 
			// Front
			p4, p5, p1, p0,
 
			// Back
			p6, p7, p3, p2,
 
			// Right
			p5, p6, p2, p1,
 
			// Top
			p7, p6, p5, p4
		};

		int[] triangles = {
			// Bottom
			3, 1, 0,
			3, 2, 1,			
 
			// Left
			3 + 4 * 1, 1 + 4 * 1, 0 + 4 * 1,
			3 + 4 * 1, 2 + 4 * 1, 1 + 4 * 1,
 
			// Front
			3 + 4 * 2, 1 + 4 * 2, 0 + 4 * 2,
			3 + 4 * 2, 2 + 4 * 2, 1 + 4 * 2,
 
			// Back
			3 + 4 * 3, 1 + 4 * 3, 0 + 4 * 3,
			3 + 4 * 3, 2 + 4 * 3, 1 + 4 * 3,
 
			// Right
			3 + 4 * 4, 1 + 4 * 4, 0 + 4 * 4,
			3 + 4 * 4, 2 + 4 * 4, 1 + 4 * 4,
 
			// Top
			3 + 4 * 5, 1 + 4 * 5, 0 + 4 * 5,
			3 + 4 * 5, 2 + 4 * 5, 1 + 4 * 5,
		};

		this.cube = new Mesh();
		this.cube.Clear();
		this.cube.vertices = vertices;
		this.cube.triangles = triangles;
		this.cube.RecalculateNormals();
	}

	void obtainMeshes()
	{
		MeshFilter[] meshes = FindObjectsOfType<MeshFilter>();

		foreach (var mesh in meshes)
		{
			var renderer = mesh.gameObject.GetComponent<Renderer>();

			if (renderer && renderer.enabled && gridBounds.Intersects(renderer.bounds))
			{
				renderer.enabled = false;
				sceneObjects.Add(mesh.gameObject);
			}
		}
	}

	void initShaders()
	{
		voxelizerMaterial = new Material(Resources.Load<Shader>("Shaders/Voxelizer"));
		drawVoxelMaterial = new Material(Resources.Load<Shader>("Shaders/DrawVoxels"));
		clearCompute = Resources.Load<ComputeShader>("Shaders/Compute/Clear3DTexture");
		clearKernelIndex = clearCompute.FindKernel("CSMain");

		occupiedVoxelCounter = Resources.Load<ComputeShader>("Shaders/Compute/occupiedVoxelCounter");
		occupiedVoxelCounterIndex = occupiedVoxelCounter.FindKernel("CSMain");

		argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
		positionBuffer = new ComputeBuffer(this.size * this.size * this.size, 3 * sizeof(uint), ComputeBufferType.Append);
		argsBuffer.SetData(args);
	}

    // Start is called before the first frame update
    void Start()
    {
		createVoxelBuffer();
		computeWorldToUnitCubeMatrix();
		obtainMeshes();
		initShaders();
		createCubeMesh();
	}

	void clearVoxelizedScene()
	{
		clearCompute.SetVector("clearColor", new Vector4(0.0f, 0.0f, 0.0f, 0.0f));
		clearCompute.SetTexture(clearKernelIndex, "output", voxelizedAlbedo);

		uint x, y, z;
		clearCompute.GetKernelThreadGroupSizes(clearKernelIndex, out x, out y, out z);
		clearCompute.Dispatch(clearKernelIndex, voxelizedAlbedo.width / (int)x, voxelizedAlbedo.height / (int)y, voxelizedAlbedo.volumeDepth / (int)z);
	}

	void voxelizeScene()
	{
		Graphics.ClearRandomWriteTargets();

		voxelizerMaterial.SetMatrix("_worldToUnitCube", worldToUnitCube);
		voxelizerMaterial.SetTexture("_voxelizedAlbedo", voxelizedAlbedo);
		voxelizerMaterial.SetTexture("_voxelizedMetallicSmoothness", voxelizedMetallicSmoothness);
		Graphics.SetRandomWriteTarget(1, voxelizedAlbedo);
		Graphics.SetRandomWriteTarget(2, voxelizedMetallicSmoothness);

		foreach(var voxelizedObject in sceneObjects)
		{
			var materials = voxelizedObject.GetComponent<Renderer>().materials;
			var mesh = voxelizedObject.GetComponent<MeshFilter>().mesh;
			int index = 0;

			foreach(var material in materials)
			{
				voxelizerMaterial.SetVector("_albedo", material.GetColor("_Color"));
				voxelizerMaterial.SetFloat("_metallic", material.GetFloat("_Metallic"));
				voxelizerMaterial.SetFloat("_smoothness", material.GetFloat("_Glossiness"));

				var mainTex = material.GetTexture("_MainTex");
				var metallicGlossMap = material.GetTexture("_MetallicGlossMap");

				voxelizerMaterial.SetInt("_useAlbedoMap", mainTex != null ? 1 : 0);
				voxelizerMaterial.SetInt("_useMetallicGlossMap", metallicGlossMap != null ? 1 : 0);
				voxelizerMaterial.SetTexture("_mainAlbedo", mainTex);
				voxelizerMaterial.SetTexture("_metallicGlossMap", metallicGlossMap);

				voxelizerMaterial.SetPass(0);

				Graphics.DrawMeshNow(mesh, voxelizedObject.transform.localToWorldMatrix, index++);
			}
		}		

		Graphics.ClearRandomWriteTargets();
	}

	void drawVoxel()
	{
		positionBuffer.SetCounterValue(0);
		occupiedVoxelCounter.SetTexture(occupiedVoxelCounterIndex, "voxelGrids", voxelizedAlbedo);
		occupiedVoxelCounter.SetBuffer(occupiedVoxelCounterIndex, "argBuffer", argsBuffer);
		occupiedVoxelCounter.SetBuffer(occupiedVoxelCounterIndex, "perInstancePosition", positionBuffer);

		uint x, y, z;
		occupiedVoxelCounter.GetKernelThreadGroupSizes(occupiedVoxelCounterIndex, out x, out y, out z);
		occupiedVoxelCounter.Dispatch(occupiedVoxelCounterIndex, voxelizedAlbedo.width / (int)x, voxelizedAlbedo.height / (int)y, voxelizedAlbedo.volumeDepth / (int)z);

		drawVoxelMaterial.SetVector("cameraPosition", Camera.main.transform.position);

		drawVoxelMaterial.SetVector("gridOffset", voxelBias);
		drawVoxelMaterial.SetFloat("voxelSize", (1.0f / voxelScale) / voxelizedAlbedo.width);

		drawVoxelMaterial.SetBuffer("voxelPositions", positionBuffer);
		drawVoxelMaterial.SetTexture("voxelizedAlbedo", voxelizedAlbedo);
		drawVoxelMaterial.SetTexture("voxelizedMetallicSmothness", voxelizedMetallicSmoothness);

		Graphics.DrawMeshInstancedIndirect(cube, 0, drawVoxelMaterial, this.gridBounds, argsBuffer);
	}

    // Update is called once per frame
    void Update()
    {
		clearVoxelizedScene();
		voxelizeScene();
		drawVoxel();
	}

	void OnDisable()
	{
		if (positionBuffer != null)
			positionBuffer.Release();
		positionBuffer = null;

		if (argsBuffer != null)
			argsBuffer.Release();
		argsBuffer = null;
	}
}
