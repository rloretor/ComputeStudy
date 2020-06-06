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

        [SerializeField] private Mesh FSQ;
        [SerializeField] private Material FSQMat;


        private int fluidDepthID = Shader.PropertyToID("FluidDepth");
        private int fluidNormalID = Shader.PropertyToID("FluidNormals");
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
                UpdateShaders();
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
            commandBuffer.GetTemporaryRT(fluidDepthID, -1, -1, 16, FilterMode.Trilinear,
                RenderTextureFormat.Depth);
            commandBuffer.GetTemporaryRT(fluidNormalID, -1, -1, 0, FilterMode.Trilinear,
                RenderTextureFormat.Default);

            commandBuffer.SetRenderTarget(new RenderTargetIdentifier[] {fluidNormalID}, fluidDepthID);
            //commandBuffer.SetRenderTarget(fluidDepthID);
            commandBuffer.ClearRenderTarget(true, true, Color.black);
            UpdateShaders();
            flockingCompute.Compute(commandBuffer, debug);
            flockingDrawer.CommandDraw(commandBuffer);
            commandBuffer.SetGlobalTexture("Fluid", fluidDepthID);
            commandBuffer.SetGlobalTexture("FluidNormals", fluidNormalID);
            commandBuffer.ReleaseTemporaryRT(fluidDepthID);
            commandBuffer.ReleaseTemporaryRT(fluidNormalID);
            //commandBuffer.SetRenderTarget(rt);
            // commandBuffer.ClearRenderTarget(false, true, Color.black);

            commandBuffer.Blit(BuiltinRenderTextureType.CurrentActive, rt, FSQMat,
                0);
            commandBuffer.SetRenderTarget(rt);
            // commandBuffer.SetGlobalTexture("Fluid", BuiltinRenderTextureType.Depth);
            // commandBuffer.Blit(BuiltinRenderTextureType.Depth, BuiltinRenderTextureType.CameraTarget, FSQMat, 1);
        }

        private void UpdateShaders()
        {
            flockingCompute.InitConstantBuffer();
            flockingDrawer.SetShader();
            FSQMat?.SetVector("_mainLightDir", mainLight.transform.forward);
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