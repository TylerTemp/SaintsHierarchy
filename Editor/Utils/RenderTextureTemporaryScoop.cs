using System;
using UnityEngine;

namespace SaintsHierarchy.Editor.Utils
{
    public readonly struct RenderTextureTemporaryScoop: IDisposable
    {
        public readonly RenderTexture RenderTex;

        public RenderTextureTemporaryScoop(int width,
            int height,
            int depthBuffer,
            RenderTextureFormat format,
            RenderTextureReadWrite readWrite)
        {
            RenderTex = RenderTexture.GetTemporary(
                width,
                height,
                depthBuffer,
                format,
                readWrite);
        }

        public void Dispose()
        {
            RenderTexture.ReleaseTemporary(RenderTex);
        }
    }
}
