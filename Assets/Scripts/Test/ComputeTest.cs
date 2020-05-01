using System;
using System.Collections;
using UnityEditor;
using UnityEngine;

public class ComputeTest : MonoBehaviour
{
    [SerializeField] private ComputeShader ComputeTestShader;
    private Vector2Int RenderTextureSize = new Vector2Int(1920, 1080);
    private Vector2Int GroupSize = new Vector2Int(32, 32);

    [SerializeField] private RenderTexture myRenderTexture;

    public IEnumerator RunCompute()
    {
        myRenderTexture = new RenderTexture(this.RenderTextureSize.x, this.RenderTextureSize.y - 1, 0);
        myRenderTexture.enableRandomWrite = true;
        myRenderTexture.Create();

        int kernelHandle = ComputeTestShader.FindKernel("CSMain");
        ComputeTestShader.SetTexture(kernelHandle, "Result", myRenderTexture);
        Shader.SetGlobalTexture("Result", myRenderTexture);
        ComputeTestShader.SetVector("RTSize", new Vector4(RenderTextureSize.x, RenderTextureSize.y, 0, 0));
        ComputeTestShader.Dispatch(kernelHandle, RenderTextureSize.x / GroupSize.x,
            Mathf.CeilToInt((float) RenderTextureSize.y / GroupSize.y),
            1);
        Debug.Log($"{RenderTextureSize.x / GroupSize.x},{RenderTextureSize.y / GroupSize.y}");
        yield return new WaitForEndOfFrame();
        SaveTexture();
    }

    public void SaveTexture()
    {
        byte[] bytes = ToTexture2D(myRenderTexture).EncodeToPNG();
        long ticks = DateTime.UtcNow.Ticks;
        Debug.Log("saving to : " + $"{Application.dataPath}/Result_{ticks}.png");
        System.IO.File.WriteAllBytes(
            $"{Application.dataPath}/Result_{ticks}.png",
            bytes);
    }

    Texture2D ToTexture2D(RenderTexture rTex)
    {
        Texture2D tex = new Texture2D(RenderTextureSize.x, RenderTextureSize.y, TextureFormat.RGB24, false);
        RenderTexture.active = rTex;
        tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
        tex.Apply();
        return tex;
    }

    public void StartComputeAndSave()
    {
        StartCoroutine(RunCompute());
    }
}

[CustomEditor((typeof(ComputeTest)))]
public class ComputeTestCustomEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        ComputeTest myTarget = target as ComputeTest;
        if (GUILayout.Button("Dispatch ComputeShader and save"))
        {
            myTarget.StartComputeAndSave();
        }
    }
}