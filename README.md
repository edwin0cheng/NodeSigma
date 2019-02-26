# NodeSigma

A collection of useful nodes and utils for Unity Shader Graph and SRP

## Requirement

* Unity 2018.3 or later
* Scriptable Render Pipeline
* Shader Graph

## Installation

Add the following line to the dependencies section in the package manifest file (Packages/manifest.json). Note that this feature is only available from Unity 2018.3. See the forum thread for further details.

```
"com.github.edwin0cheng.nodesigma": "https://github.com/edwin0cheng/NodeSigma.git#upm",
```


## Nodes

| Name | Node            | Note  |
:-------------------------:|:-------------------------:|:-------------------------:
Edge Detection | <img src ="https://i.imgur.com/EEoSMbc.png" width="300"> |<img src="https://i.imgur.com/SeTLqms.png" width="300">
Depth Normals Texture | <img src ="https://i.imgur.com/ZZ08Blh.png" width="300"> | As LWRP do not support Depth Normals Texture, A `DephtNormalsRenderPass` is required to add in the game object of main camera
