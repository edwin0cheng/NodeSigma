using UnityEditor.ShaderGraph;

namespace NodeSigma.Nodes.Editor
{
    [Title("NodeSigma", "Input", "Depth Normals Texture")]
    public class DepthNormalsTextureNode : CustomTextureNode
    {
        public DepthNormalsTextureNode() : base("_CameraDepthNormalsTexture")
        {
            name = "Depth Normals Texture";
        }
    }
}