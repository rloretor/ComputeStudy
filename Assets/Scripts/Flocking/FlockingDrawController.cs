using System;
using UnityEngine;
using UnityEngine.Rendering;

[Serializable]
public class FlockingDrawController
{
    private Material boidDrawMaterial;

    [Header("Flocking Drawer")] [SerializeField]
    private Shader BoidInstanceShader;

    [SerializeField] private Mesh boidMesh;
    private BoidModel boidModel;
    [SerializeField] private bool isbillboard = true;
    [SerializeField] [Range(0, 5.0f)] private float particleSize;
    [SerializeField] private Gradient ColorPalette;

    [SerializeField] private Texture2D ColorPaletteTexture;

    public void Init(BoidModel boidModel, CommandBuffer command = null)
    {
        this.boidModel = boidModel;

        InitTexture();

        boidDrawMaterial = new Material(BoidInstanceShader)
        {
            name = "Boids",
            hideFlags = HideFlags.HideAndDontSave,
            enableInstancing = true
        };

        if (command == null)
        {
            boidDrawMaterial.SetBuffer("_BoidsBuffer", boidModel.BoidBuffer);
            boidDrawMaterial.SetInt("_Instances", boidModel.instances);
        }
        else
        {
            command.SetGlobalBuffer("_BoidsBuffer", boidModel.BoidBuffer);
            command.SetGlobalInt("_BoidsBuffer", boidModel.instances);
        }
    }

    private void InitTexture()
    {
        ColorPaletteTexture = new Texture2D(10, 1);

        ColorPaletteTexture.SetPixels(new[]
        {
            ColorPalette.Evaluate(0),
            ColorPalette.Evaluate(0.1f),
            ColorPalette.Evaluate(0.2f),
            ColorPalette.Evaluate(0.3f),
            ColorPalette.Evaluate(0.4f),
            ColorPalette.Evaluate(0.5f),
            ColorPalette.Evaluate(0.6f),
            ColorPalette.Evaluate(0.7f),
            ColorPalette.Evaluate(0.8f),
            ColorPalette.Evaluate(0.9f),
            ColorPalette.Evaluate(1),
        });
        ColorPaletteTexture.filterMode = FilterMode.Bilinear;
        ColorPaletteTexture.Apply();
    }

    public void SetShader()
    {
        boidDrawMaterial.SetTexture("_colorPalette", ColorPaletteTexture);
        boidDrawMaterial.SetFloat("_SphereRadius", particleSize / 2.0f);

        if (isbillboard)
            boidDrawMaterial.EnableKeyword("ISBILLBOARD");

        else
            boidDrawMaterial.DisableKeyword("ISBILLBOARD");
    }

    private void CommandSetShader(CommandBuffer command)
    {
        command.SetGlobalFloat("_SphereRadius", particleSize / 2.0f);

        if (isbillboard)
            command.EnableShaderKeyword("ISBILLBOARD");
        else
            command.DisableShaderKeyword("ISBILLBOARD");
    }

    public void Draw()
    {
        SetShader();
        Graphics.DrawMeshInstancedProcedural(boidMesh, 0, boidDrawMaterial, boidModel.boidBounds, boidModel.instances,
            null,
            ShadowCastingMode.Off, false);
        //Graphics.DrawMeshInstancedIndirect(BoidMesh, 0, BoidDrawMaterial, boidBounds, BufferWithArgs);
    }

    public void CommandDraw(CommandBuffer command,int pass = 0 )
    {
        command.DrawMeshInstancedProcedural(boidMesh, 0, boidDrawMaterial, pass, boidModel.instances);
    }
}