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


    public void Init(BoidModel boidModel, CommandBuffer command = null)
    {
        this.boidModel = boidModel;

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

    public void SetShader()
    {
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

    public void CommandDraw(CommandBuffer command)
    {
        command.DrawMeshInstancedProcedural(boidMesh, 0, boidDrawMaterial, 0, boidModel.instances);
    }
}