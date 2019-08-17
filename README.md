# Cookie-Baker-RT
I'm thinking of using graphical ray-tracing within Unity to create Cookie Textures that will act as shadow masks for some of the light types.

## Current Status:

It turns out that I am unable to use Unity's build-in Compute Shaders to offload the work to the GPU without freezing the Unity Editor. I asked around, and it looks like other groups in Unity have gotten around this issue by running OpenCL from a background thread. I see no reason why I can just create a Unity based application and run that from a background thread; it should be faster for me to develop than learning to create an OpenCL application.

## Requirements:

Google's flatbuffers

If you want to switch-out one of the processes while maintaing some of the same interprocess-communication (IPC) that I'm using, then you will need to compile a new set data serializers if you want to create a process that isn't C# based. If you do end up doing this, you should also compile a new set of C# data serializers to make sure that you are using same version of flatbuffers for all of the IPC code.
