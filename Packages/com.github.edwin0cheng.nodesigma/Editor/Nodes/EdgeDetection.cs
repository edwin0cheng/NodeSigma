using System.Reflection;
using UnityEngine;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.Graphing;

using NodeSigma.RenderPass.Runtime;

namespace NodeSigma.Nodes.Editor
{
    [Title("NodeSigma", "Effect", "Edge Detection")]
    public class EdgeDetectionNode : CodeFunctionNode
    {
        public EdgeDetectionNode()
        {
            name = "Edge Detection";
        }

        [SerializeField]
        private DepthNormalsRenderPass m_GameObject;

        // public override string documentationURL
        // {
        //     get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Gradient-Noise-Node"; }
        // }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("NodeSigma_EdgeDetection", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string NodeSigma_EdgeDetection(            
            [Slot(0, Binding.ScreenPosition)] Vector4 ScreenPos,
            [Slot(1, Binding.None)] ColorRGBA Color,
            [Slot(2, Binding.None)] Texture2D DepthTexture,
            [Slot(3, Binding.None)] SamplerState DepthTextureState,
            [Slot(4, Binding.None)] Vector2 DepthTextureTexel,
            [Slot(5, Binding.None, 2.0f, 0, 0, 0)] Vector1 SampleDistance,
            [Slot(6, Binding.None, 10.0f, 0, 0, 0)] Vector1 Falloff,
            [Slot(7, Binding.None, 0.82f, 0, 0, 0)] Vector1 SensitivityNormals,
            [Slot(8, Binding.None, 3.75f, 0, 0, 0)] Vector1 SensitivityDepth,            
            [Slot(9, Binding.None)] out Vector4 Out)
        {
            Out = Vector4.zero;


            return @"
{
    Out = {precision}4(nodesigma_edgecolor(ScreenPos, Color, DepthTexture, DepthTextureState, DepthTextureTexel, SampleDistance, Falloff, SensitivityNormals, SensitivityDepth));    
}
";
        }

        public override void GenerateNodeFunction(FunctionRegistry registry, GraphContext graphContext, GenerationMode generationMode)
        {            
            registry.ProvideFunction("ns_DecodeFloatRG", s => s.Append(@"
float ns_DecodeFloatRG( float2 enc )
{
    float2 kDecodeDot = float2(1.0, 1/255.0);
    return dot( enc, kDecodeDot );
}
"));

            registry.ProvideFunction("nodesigma_CheckSame", s => s.Append(@"
float nodesigma_CheckSame(float2 centerNormal, float centerDepth, float4 theSample, 
    float sensitivityNormals, float sensitivityDepth)
{
    // differene in normals
    // do not bother decoding normals - there's no need here
    float2 diff = abs(centerNormal - theSample.xy) * sensitivityNormals;
    int isSameNormal = (diff.x + diff.y) < 0.1;

    // differenece in depth
    float sampleDepth = ns_DecodeFloatRG(theSample.zw);
    float zdiff = abs(centerDepth - sampleDepth);
    // scale the requireed threshold by the distance
    int isSameDepth = zdiff * sensitivityDepth < 0.99 * centerDepth;

    // return:
    // 1 - if normals and depth are similar enough
    // 0 - otherwise

    return (isSameNormal * isSameDepth) ? 1.0 : 0.0;

}
"));

            registry.ProvideFunction("nodesigma_edgecolor", s => s.Append(@"
float4 nodesigma_edgecolor(float4 screenPos, float4 color, Texture2D depthTex, 
    SamplerState depthTexState, float2 depthTexelSize, 
    float sampleDistance, float falloff, float sensitivityNormals, float sensitivityDepth)
{
    float sampleSizeX = depthTexelSize.x;
    float sampleSizeY = depthTexelSize.y;
    float2 screenUV = screenPos.xy;

    float2 _uv2 = screenUV + float2(-sampleSizeX, +sampleSizeY) * sampleDistance;
    float2 _uv3 = screenUV + float2(+sampleSizeX, -sampleSizeY) * sampleDistance;
    float2 _uv4 = screenUV + float2( sampleSizeX,  sampleSizeY) * sampleDistance;
    float2 _uv5 = screenUV + float2(-sampleSizeX, -sampleSizeY) * sampleDistance;

    float4 center = SAMPLE_TEXTURE2D(depthTex, depthTexState, screenUV);
    float4 sample1 = SAMPLE_TEXTURE2D(depthTex, depthTexState, _uv2);
    float4 sample2 = SAMPLE_TEXTURE2D(depthTex, depthTexState, _uv3);
    float4 sample3 = SAMPLE_TEXTURE2D(depthTex, depthTexState, _uv4);
    float4 sample4 = SAMPLE_TEXTURE2D(depthTex, depthTexState, _uv5);    

    float edge = 1.0;

    // encode normal
    float2 centerNormal = center.xy;
    //decode depth
    float centerDepth = ns_DecodeFloatRG(center.zw);

    // // calculate how faded the edge is
    float d = clamp(centerDepth * falloff - 0.05, 0.0, 1.0);    
    float4 depthFade = float4(d, d, d, 1.0);

    // is it an edge? 0 if yes, 1 if no    
    edge *= nodesigma_CheckSame(centerNormal, centerDepth, sample1, sensitivityNormals, sensitivityDepth);
    edge *= nodesigma_CheckSame(centerNormal, centerDepth, sample2, sensitivityNormals, sensitivityDepth);
    edge *= nodesigma_CheckSame(centerNormal, centerDepth, sample3, sensitivityNormals, sensitivityDepth);
    edge *= nodesigma_CheckSame(centerNormal, centerDepth, sample4, sensitivityNormals, sensitivityDepth);

    return edge * color  + (1.0 - edge) * (depthFade * color);
}
"));
    


            base.GenerateNodeFunction(registry, graphContext, generationMode);
        }
    }
}