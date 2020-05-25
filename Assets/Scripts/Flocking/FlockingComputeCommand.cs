using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Flocking
{
    public class FlockingComputeCommand : MonoBehaviour
    {
        private Dictionary<Camera, CommandBuffer> commandBufferMap = new Dictionary<Camera, CommandBuffer>();


        [Header("Rendering")] [SerializeField] private CameraEvent WhenToRender;


        private void TryAddCommandBuffer()
        {
            var cam = Camera.current;

            CommandBuffer commandBuffer = null;
            if (commandBufferMap.ContainsKey(cam))
            {
                commandBufferMap[cam].Clear();
                return;
            }

            commandBuffer = new CommandBuffer
            {
                name = cam.name + " " + GetType()
            };

            cam.AddCommandBuffer(WhenToRender, commandBuffer);
            commandBufferMap.Add(cam, commandBuffer);

            foreach (var entry in commandBufferMap) Debug.Log($"{entry.Key.name} has {entry.Value.name} associated");
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
        }
    }
}