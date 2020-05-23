using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BilateralBlur : MonoBehaviour
{
    [SerializeField] private Material postprocessMaterial;

    void Start()
    {
        var _camera = Camera.current;

        _camera.depthTextureMode = _camera.depthTextureMode | DepthTextureMode.Depth;
        ;
    }

    //method which is automatically called by unity after the camera is done rendering
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        //draws the pixels from the source texture to the destination texture
        var temporaryTexture = RenderTexture.GetTemporary(source.width, source.height, source.depth);
        var matrix = GetComponent<Camera>().cameraToWorldMatrix;
        postprocessMaterial.SetMatrix("_InverseView", matrix);
        Graphics.Blit(source, temporaryTexture, postprocessMaterial, 0);
        Graphics.Blit(temporaryTexture, destination, postprocessMaterial, 1);
        RenderTexture.ReleaseTemporary(temporaryTexture);
    }
}