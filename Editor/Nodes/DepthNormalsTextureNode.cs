using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;
using System.Reflection;


namespace NodeSigma.Nodes.Editor
{
        public class DepthNormalsTextureControlAttribute : Attribute, IControlAttribute
    {
        public VisualElement InstantiateControl(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            if (!(node is DepthNormalsTextureNode))
                throw new ArgumentException("Node must inherit from DepthNormalsTextureNode.", "node");
            return new DepthNormalsTextureControlView((DepthNormalsTextureNode)node);
        }
    }

    public class DepthNormalsTextureControlView : VisualElement
    {
        DepthNormalsTextureNode m_Node;

        void UpdateError(string err)
        {           
            TextElement textEle = this.Children().FirstOrDefault( e => e.name == "ErrorText") as TextElement;

            if(err != null)       
            {
                if(textEle == null)
                {
                    textEle = new TextElement() { name = "ErrorText"};
                    Add(textEle);
                }

                textEle.text = err;                    
            } 
            else if(textEle != null)
            {
                Remove(textEle);
            }
        }

        public DepthNormalsTextureControlView(DepthNormalsTextureNode node)
        {
            m_Node = node;
            AddStyleSheetPath("Styles/Controls/DepthNormalsTextureControlView");
            
            m_Node.OnError = UpdateError;

            // Force Update the Error as Validate() may call before this view creation.
            UpdateError(m_Node.errorString);
        }        
    }


    [Title("NodeSigma", "Input", "Depth Normals Texture")]
    public class DepthNormalsTextureNode : CustomTextureNode
    {
        [DepthNormalsTextureControl]
        int controlDummy { get; set; }
        
        [NonSerialized]
        internal Action<string> OnError;

        internal string errorString;

        public DepthNormalsTextureNode() : base("_CameraDepthNormalsTexture")
        {
            name = "Depth Normals Texture";
        }

        public override void ValidateNode() 
        {
            bool foundTexture = Shader.GetGlobalTexture("_CameraDepthNormalsTexture") != null;
            errorString = foundTexture ? null : "Error: No Depth Normals Texture was found!";

            if(OnError != null)
                OnError(errorString);

            base.ValidateNode();
        }
    }
}