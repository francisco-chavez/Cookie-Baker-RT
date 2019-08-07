# Cookie-Baker-RT
I'm thinking of using graphical ray-tracing within Unity to create Cookie Textures that will act as shadow masks for some of the light types.

-Current Status

It turns out that I am unable to use Unity's build-in Compute Shaders to offload the work to the GPU without freezing the Unity Editor. I asked around, and it looks like other groups in Unity have gotten around this issue by running OpenCL from a background thread. I've never used OpenCL before, so I've got some learning to do. In the meantime, I will continue to develop this plug-in with CPU based code.
