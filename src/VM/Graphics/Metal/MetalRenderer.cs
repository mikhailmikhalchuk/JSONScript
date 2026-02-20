using System;
using System.Runtime.InteropServices;

namespace JSONScript.VM.Graphics.Metal
{
    public partial class MetalRenderer
    {
        /// <summary>
        /// Returns the device instance Metal selects as the default.
        /// </summary>
        /// <returns>A device object.</returns>
        [LibraryImport("/System/Library/Frameworks/Metal.framework/Metal")]
        private static partial IntPtr MTLCreateSystemDefaultDevice();

        /// <summary>
        /// A Core Animation layer that Metal can render into, typically displayed onscreen.
        /// </summary>
        /// <returns></returns>
        [LibraryImport("/System/Library/Frameworks/QuartzCore.framework/QuartzCore")]
        private static partial IntPtr CAMetalLayerClass();

        public nint LayerPtr  { get; private set; }
        public nint DevicePtr { get; private set; }

        private readonly IntPtr device;
        private readonly IntPtr commandQueue;
        private readonly IntPtr metalLayer;
        private IntPtr pipelineState;

        public MetalRenderer(IntPtr view, int width, int height)
        {
            device = MTLCreateSystemDefaultDevice();
            if (device == IntPtr.Zero)
                throw new Exception("Failed to create Metal device");

            DevicePtr = device;  // add this

            var newCommandQueue = ObjC.RegisterName("newCommandQueue");
            commandQueue = ObjC.MsgSend(device, newCommandQueue);

            var caMetalLayerClass = ObjC.GetClass("CAMetalLayer");
            var layerAlloc = ObjC.RegisterName("alloc");
            var layerInit  = ObjC.RegisterName("init");
            metalLayer = ObjC.MsgSend(ObjC.MsgSend(caMetalLayerClass, layerAlloc), layerInit);

            LayerPtr = metalLayer;  // add this

            //set device on layer
            var setDevice = ObjC.RegisterName("setDevice:");
            ObjC.MsgSendVoid(metalLayer, setDevice, device);

            //set pixel format (BGRA8Unorm = 80)
            var setPixelFormat = ObjC.RegisterName("setPixelFormat:");
            ObjC.MsgSendVoid(metalLayer, setPixelFormat, 80);

            //set drawable size
            var setDrawableSize = ObjC.RegisterName("setDrawableSize:");
            ObjC.MsgSendVoid(metalLayer, setDrawableSize, new CGSize(width, height));

            //attach layer to view
            var setWantsLayer = ObjC.RegisterName("setWantsLayer:");
            ObjC.MsgSendVoid(view, setWantsLayer, true);

            var setLayer = ObjC.RegisterName("setLayer:");
            ObjC.MsgSendVoid(view, setLayer, metalLayer);

            //compile shaders and create pipeline
            CreatePipeline();
        }

        private void CreatePipeline()
        {
            //Metal Shading Language (msl) shader source
            string shaderSource = @"
                #include <metal_stdlib>
                using namespace metal;

                struct Vertex {
                    float2 position;
                    float4 color;
                };

                struct VertexOut {
                    float4 position [[position]];
                    float4 color;
                };

                vertex VertexOut vertexShader(
                    uint vertexID [[vertex_id]],
                    constant float2* positions [[buffer(0)]],
                    constant float4* colors    [[buffer(1)]])
                {
                    VertexOut out;
                    out.position = float4(positions[vertexID], 0.0, 1.0);
                    out.color    = colors[vertexID];
                    return out;
                }

                fragment float4 fragmentShader(VertexOut in [[stage_in]])
                {
                    return in.color;
                }
            ";

            //create nsstring from shader source
            var nsStringClass  = ObjC.GetClass("NSString");
            var stringWithUTF8 = ObjC.RegisterName("stringWithUTF8String:");
            var sourcePtr = Marshal.StringToHGlobalAnsi(shaderSource);
            var nsSource = ObjC.MsgSend(nsStringClass, stringWithUTF8, sourcePtr);
            Marshal.FreeHGlobal(sourcePtr);

            //compile shader library
            var newLibraryWithSource = ObjC.RegisterName("newLibraryWithSource:options:error:");
            var library = ObjC.MsgSend(device, newLibraryWithSource, nsSource, IntPtr.Zero, IntPtr.Zero);
            if (library == IntPtr.Zero)
                throw new Exception("Failed to compile Metal shaders");

            //get vertex and fragment functions
            var newFunctionWithName = ObjC.RegisterName("newFunctionWithName:");

            var vertexNamePtr = Marshal.StringToHGlobalAnsi("vertexShader");
            var fragmentNamePtr = Marshal.StringToHGlobalAnsi("fragmentShader");

            var vertexNSString = ObjC.MsgSend(nsStringClass, stringWithUTF8, vertexNamePtr);
            var fragmentNSString = ObjC.MsgSend(nsStringClass, stringWithUTF8, fragmentNamePtr);

            Marshal.FreeHGlobal(vertexNamePtr);
            Marshal.FreeHGlobal(fragmentNamePtr);

            var vertexFunction = ObjC.MsgSend(library, newFunctionWithName, vertexNSString);
            var fragmentFunction = ObjC.MsgSend(library, newFunctionWithName, fragmentNSString);

            //create pipeline descriptor
            var pipelineDescClass = ObjC.GetClass("MTLRenderPipelineDescriptor");
            var pipelineDescAlloc = ObjC.RegisterName("alloc");
            var pipelineDescInit = ObjC.RegisterName("init");
            var pipelineDesc = ObjC.MsgSend(ObjC.MsgSend(pipelineDescClass, pipelineDescAlloc), pipelineDescInit);

            //set vertex and fragment functions
            var setVertexFunction = ObjC.RegisterName("setVertexFunction:");
            var setFragmentFunction = ObjC.RegisterName("setFragmentFunction:");
            ObjC.MsgSendVoid(pipelineDesc, setVertexFunction, vertexFunction);
            ObjC.MsgSendVoid(pipelineDesc, setFragmentFunction, fragmentFunction);

            //create vertex descriptor to describe memory layout
            var vertexDescClass = ObjC.GetClass("MTLVertexDescriptor");
            var vertexDescriptor = ObjC.MsgSend(vertexDescClass, ObjC.RegisterName("vertexDescriptor"));

            //position attribute — offset 0, 2 floats
            var attributes = ObjC.RegisterName("attributes");
            var attrArray = ObjC.MsgSend(vertexDescriptor, attributes);
            var attr0 = ObjC.MsgSendIndex(attrArray, ObjC.RegisterName("objectAtIndexedSubscript:"), 0UL);

            ObjC.MsgSendVoid(attr0, ObjC.RegisterName("setFormat:"), 30); //mtlvertexformatfloat2 = 30
            ObjC.MsgSendVoid(attr0, ObjC.RegisterName("setOffset:"), 0);
            ObjC.MsgSendVoid(attr0, ObjC.RegisterName("setBufferIndex:"), 0);

            //color attribute — offset 8 (2 floats * 4 bytes), 4 floats
            var attr1 = ObjC.MsgSendIndex(attrArray, ObjC.RegisterName("objectAtIndexedSubscript:"), 1UL);
            ObjC.MsgSendVoid(attr1, ObjC.RegisterName("setFormat:"), 34); //mtlvertexformatfloat4 = 34
            ObjC.MsgSendVoid(attr1, ObjC.RegisterName("setOffset:"), 8);  //2 floats * 4 bytes
            ObjC.MsgSendVoid(attr1, ObjC.RegisterName("setBufferIndex:"), 0);

            //layout — stride is 24 bytes (6 floats * 4 bytes)
            var layouts = ObjC.RegisterName("layouts");
            var layoutArray = ObjC.MsgSend(vertexDescriptor, layouts);
            var layout0 = ObjC.MsgSendIndex(layoutArray, ObjC.RegisterName("objectAtIndexedSubscript:"), 0UL);
            ObjC.MsgSendVoid(layout0, ObjC.RegisterName("setStride:"), 24); //6 floats * 4 bytes

            //attach vertex descriptor to pipeline descriptor
            var setVertexDescriptor = ObjC.RegisterName("setVertexDescriptor:");
            ObjC.MsgSendVoid(pipelineDesc, setVertexDescriptor, vertexDescriptor);

            //set pixel format on color attachment
            var colorAttachments = ObjC.RegisterName("colorAttachments");
            var objectAtIndex = ObjC.RegisterName("objectAtIndexedSubscript:");
            var colorAttachment0 = ObjC.MsgSend(ObjC.MsgSend(pipelineDesc, colorAttachments), objectAtIndex, IntPtr.Zero);
            var setPixelFormatAttach = ObjC.RegisterName("setPixelFormat:");
            ObjC.MsgSendVoid(colorAttachment0, setPixelFormatAttach, 80);

            //create pipeline state
            var newPipelineState = ObjC.RegisterName("newRenderPipelineStateWithDescriptor:error:");
            pipelineState = ObjC.MsgSend(device, newPipelineState, pipelineDesc, IntPtr.Zero);
            if (pipelineState == IntPtr.Zero)
                throw new Exception("Failed to create Metal pipeline state");
        }

        public void DrawRect(float x, float y, float w, float h, float r, float g, float b)
        {
            //convert pixel coords to ndc (-1 to 1)
            float ndcX = (x / 400f) - 1f;
            float ndcY = 1f - (y / 300f);
            float ndcW = w / 400f;
            float ndcH = h / 300f;

            float[] positions = [
                ndcX, ndcY,
                ndcX + ndcW, ndcY,
                ndcX, ndcY - ndcH,
                ndcX + ndcW, ndcY,
                ndcX + ndcW, ndcY - ndcH,
                ndcX, ndcY - ndcH,
            ];

            float[] colorData = [
                r, g, b, 1f,
                r, g, b, 1f,
                r, g, b, 1f,
                r, g, b, 1f,
                r, g, b, 1f,
                r, g, b, 1f,
            ];

            //get next drawable from Metal layer
            var nextDrawable = ObjC.RegisterName("nextDrawable");
            var drawable = ObjC.MsgSend(metalLayer, nextDrawable);
            if (drawable == IntPtr.Zero)
                return;

            var texture = ObjC.RegisterName("texture");
            var drawableTex = ObjC.MsgSend(drawable, texture);

            //create render pass descriptor
            var renderPassDescClass = ObjC.GetClass("MTLRenderPassDescriptor");
            var renderPassDescriptor = ObjC.RegisterName("renderPassDescriptor");
            var passDesc = ObjC.MsgSend(renderPassDescClass, renderPassDescriptor);

            //configure color attachment
            var colorAttachments = ObjC.RegisterName("colorAttachments");
            var objectAtIndex = ObjC.RegisterName("objectAtIndexedSubscript:");
            var colorAttach = ObjC.MsgSend(ObjC.MsgSend(passDesc, colorAttachments), objectAtIndex, IntPtr.Zero);

            var setTexture = ObjC.RegisterName("setTexture:");
            var setLoadAction = ObjC.RegisterName("setLoadAction:");
            var setStoreAction = ObjC.RegisterName("setStoreAction:");
            ObjC.MsgSendVoid(colorAttach, setTexture, drawableTex);
            ObjC.MsgSendVoid(colorAttach, setLoadAction, 2);
            ObjC.MsgSendVoid(colorAttach, setStoreAction, 1);

            //create command buffer
            var commandBuffer = ObjC.RegisterName("commandBuffer");
            var cmdBuffer = ObjC.MsgSend(commandQueue, commandBuffer);

            //create render command encoder
            var renderCommandEncoderWithDescriptor = ObjC.RegisterName("renderCommandEncoderWithDescriptor:");
            var encoder = ObjC.MsgSend(cmdBuffer, renderCommandEncoderWithDescriptor, passDesc);

            //set pipeline state
            var setRenderPipelineState = ObjC.RegisterName("setRenderPipelineState:");
            ObjC.MsgSendVoid(encoder, setRenderPipelineState, pipelineState);

            var setVertexBytes = ObjC.RegisterName("setVertexBytes:length:atIndex:");

            //upload positions to buffer 0
            var posBytes = MemoryMarshal.Cast<float, byte>(positions.AsSpan()).ToArray();
            var posHandle = GCHandle.Alloc(posBytes, GCHandleType.Pinned);
            ObjC.MsgSendVoid(encoder, setVertexBytes, posHandle.AddrOfPinnedObject(), positions.Length * sizeof(float), 0);
            posHandle.Free();

            //upload colors to buffer 1
            var colorBytes = MemoryMarshal.Cast<float, byte>(colorData.AsSpan()).ToArray();
            var colorHandle = GCHandle.Alloc(colorBytes, GCHandleType.Pinned);
            ObjC.MsgSendVoid(encoder, setVertexBytes, colorHandle.AddrOfPinnedObject(), colorData.Length * sizeof(float), 1);
            colorHandle.Free();

            //draw primitives (triangle = 3, 6 vertices)
            var drawPrimitives = ObjC.RegisterName("drawPrimitives:vertexStart:vertexCount:");
            ObjC.MsgSendVoid(encoder, drawPrimitives, 3, IntPtr.Zero, 6);

            //end encoding
            var endEncoding = ObjC.RegisterName("endEncoding");
            ObjC.MsgSendVoid(encoder, endEncoding);

            //present and commit
            var presentDrawable = ObjC.RegisterName("presentDrawable:");
            ObjC.MsgSendVoid(cmdBuffer, presentDrawable, drawable);

            var commit = ObjC.RegisterName("commit");
            ObjC.MsgSendVoid(cmdBuffer, commit);
        }
    }
}