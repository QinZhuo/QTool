using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool;

public class QComputeShaderTest : MonoBehaviour
{
	public ComputeShader computeShader;
	public ComputeBuffer floatBuffer;
	public Texture3D textureTest;
	float[] floatList;
	[QTool.QName]
	public void Test()
	{
		var Size = 8*20;
		floatList = new float[Size * Size * Size];
		Tool.RunTimeCheck("普通", () =>
		{
			for (int i = 0; i < Size; i++)
			{
				for (int j = 0; j < Size; j++)
				{
					for (int w = 0; w < Size; w++)
					{
						floatList[i + Size * j + Size * Size * w] = i + j * 100 + w * 10000;
					}
				}
			}
		});
		int kernelHandle = computeShader.FindKernel("CSMain");
		Tool.RunTimeCheck(nameof(computeShader), () =>
		{
			if (floatBuffer == null)
			{
				floatBuffer = new ComputeBuffer(Size * Size * Size, 4);
			}
			computeShader.SetInt(nameof(Size), Size);
			computeShader.SetTexture(kernelHandle, nameof(textureTest), textureTest);
			computeShader.SetBuffer(kernelHandle, nameof(floatBuffer), floatBuffer);
			computeShader.Dispatch(kernelHandle,Mathf.CeilToInt(Size / 8), Size / 8, Size / 8);
			floatBuffer.GetData(floatList);
		});
	}

}
