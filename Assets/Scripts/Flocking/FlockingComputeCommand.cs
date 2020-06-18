using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Flocking
{
    public class FlockingComputeCommand : MonoBehaviour
    {
        public Light mainLight;
        public bool debug;
        private Dictionary<Camera, CommandBuffer> commandBufferMap = new Dictionary<Camera, CommandBuffer>();
        [Header("Rendering")] [SerializeField] private CameraEvent WhenToRender;

        [SerializeField] private BoidModel boidModel;

        [SerializeField] private FlockingComputeController flockingCompute;
        [SerializeField] private FlockingDrawController flockingDrawer;

        [SerializeField] private Material FSQMat;


        private int fluidDepthID = Shader.PropertyToID("FluidDepth");
        private int fluidLightDepthID = Shader.PropertyToID("FluidLightDepth");
        private int fluidNormalID = Shader.PropertyToID("FluidNormals");
        private int fluidColorID = Shader.PropertyToID("FluidColor");
        private readonly int LightVMatrixId = Shader.PropertyToID("_lightV");

        RenderTargetIdentifier[] fluidRenderTargets;


        private void OnDrawGizmos()
        {
            if (isActiveAndEnabled)
            {
                Gizmos.color = Color.black;
                Gizmos.DrawWireCube(boidModel.boidBounds.center, boidModel.boidBounds.size);
            }
        }


        private void Start()
        {
            Init();
        }

        private void Init()
        {
            boidModel.Init();
            flockingCompute.Init(boidModel);
            flockingDrawer.Init(boidModel);
            CleanNullCameras();
            SetRenderCommand();
        }

        public void OnDisable()
        {
            Cleanup();
        }

        public void OnDestroy()
        {
            Cleanup();
        }


        public void OnWillRenderObject()
        {
            if (!isActiveAndEnabled)
            {
                Cleanup();
                return;
            }
#if UNITY_EDITOR
            CleanNullCameras();
            SetRenderCommand();
#endif
        }


        private void SetRenderCommand()
        {
            var cam = Camera.current;
            if (!cam)
            {
                return;
            }

            CommandBuffer commandBuffer = null;
            if (commandBufferMap.ContainsKey(cam))
            {
                UpdateShaders(commandBuffer);
                return;
            }

            commandBuffer = new CommandBuffer
            {
                name = cam.name + " " + GetType()
            };

            cam.AddCommandBuffer(WhenToRender, commandBuffer);
            commandBufferMap.Add(cam, commandBuffer);
            ConfigureDrawing(cam);
            foreach (var entry in commandBufferMap) Debug.Log($"{entry.Key.name} has {entry.Value.name} associated");
        }

        private void ConfigureDrawing(Camera cam)
        {
            if (cam == null)
            {
                return;
            }

            var commandBuffer = commandBufferMap[cam];
            if (commandBuffer == null)
            {
                return;
            }

            var rt = RenderTexture.active;
            commandBuffer.GetTemporaryRT(fluidDepthID, -1, -1, 32, FilterMode.Bilinear,
                RenderTextureFormat.Depth);
            commandBuffer.GetTemporaryRT(fluidLightDepthID, -1, -1, 32, FilterMode.Bilinear,
                RenderTextureFormat.Depth);
            commandBuffer.GetTemporaryRT(fluidNormalID, -1, -1, 0, FilterMode.Bilinear,
                RenderTextureFormat.Default);
            commandBuffer.GetTemporaryRT(fluidColorID, -1, -1, 0, FilterMode.Bilinear,
                RenderTextureFormat.Default);

            commandBuffer.SetGlobalTexture("Fluid", fluidDepthID);
            commandBuffer.SetGlobalTexture("FluidNormals", fluidNormalID);
            commandBuffer.SetGlobalTexture("FluidColors", fluidColorID);
            // commandBuffer.SetGlobalTexture("FluidLight", fluidLightDepthID);

            var rts = new RenderTargetIdentifier[] {fluidNormalID, fluidColorID};
            commandBuffer.SetRenderTarget(rts, fluidLightDepthID);
            UpdateShaders(commandBuffer);
            // flockingCompute.Compute(commandBuffer, debug);
            flockingDrawer.CommandDraw(commandBuffer, 1);
            commandBuffer.ClearRenderTarget(true, true, Color.black);

            commandBuffer.SetRenderTarget(rts, fluidDepthID);
            //commandBuffer.SetRenderTarget(fluidDepthID);
            commandBuffer.ClearRenderTarget(true, true, Color.black);
            UpdateShaders(commandBuffer);
            // flockingCompute.Compute(commandBuffer, debug);
            flockingDrawer.CommandDraw(commandBuffer);
            //commandBuffer.ClearRenderTarget(true, true, Color.black);
            //flockingDrawer.CommandDraw(commandBuffer);
            commandBuffer.ReleaseTemporaryRT(fluidDepthID);
            commandBuffer.ReleaseTemporaryRT(fluidNormalID);

            commandBuffer.Blit(BuiltinRenderTextureType.CurrentActive, rt, FSQMat,
                0);
            commandBuffer.SetRenderTarget(rt);
        }

        private void SetLightV(CommandBuffer buffer)
        {
            bool d3d = SystemInfo.graphicsDeviceVersion.IndexOf("Direct3D") > -1;
            var V = mainLight.transform.worldToLocalMatrix;

            if (d3d)
            {
                // Invert XY for rendering to a render texture
                for (int i = 0; i < 4; i++)
                {
                    V[2, i] = -V[2, i];
                }
            }

            buffer?.SetGlobalMatrix(LightVMatrixId, V);
        }

        private void UpdateShaders(CommandBuffer buffer)
        {
            flockingCompute.InitConstantBuffer();
            flockingDrawer.SetShader();
            FSQMat?.SetVector("_mainLightDir", mainLight.transform.forward);
            SetLightV(buffer);
        }

        private void CleanNullCameras()
        {
            commandBufferMap = commandBufferMap.Select(x => x).Where(x => x.Key != null)
                .ToDictionary(x => x.Key, x => x.Value);
        }

        [ContextMenu("Cleanup")]
        private void Cleanup()
        {
            foreach (var entry in commandBufferMap)
            {
                entry.Value.Release();
                if (entry.Key == null) continue;
                entry.Key.RemoveAllCommandBuffers();
            }

            commandBufferMap.Clear();
            flockingCompute?.Dispose();
            boidModel?.Dispose();
            flockingCompute = null;
            flockingDrawer = null;
        }

        public void OnApplicationQuit()
        {
            Cleanup();
        }
    }
}