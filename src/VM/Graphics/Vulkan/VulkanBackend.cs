using System;
using System.Runtime.InteropServices;
using JSONScript.VM.Graphics.Metal;
using JSONScript.Runtime;

namespace JSONScript.VM.Graphics.Vulkan
{
    public unsafe class VulkanBackend : IGraphicsBackend
    {
        private AppKitWindow? window;
        private VulkanDevice? device;
        private VulkanSwapchain? swapchain;
        private VulkanPipeline? pipeline;
        private VulkanCommands? commands;

        //vertex buffer for drawing rects
        private IntPtr vertexBuffer;
        private IntPtr vertexBufferMemory;
        private const int MaxVertices = 6 * 1024; //enough for 1024 rects per frame

        private int width;
        private int height;

        public void Init(int width, int height, string title)
        {
            this.width = width;
            this.height = height;

            window = new AppKitWindow(width, height, title);
            window.HandleWindowClose();

            device = new VulkanDevice();
            device.Init();

            AttachMetalLayer(); //must be before CreateSurface
            CreateSurface(out var surface);

            device.InitSurface(surface);

            swapchain = new VulkanSwapchain(device, surface);
            swapchain.Init(width, height);

            pipeline = new VulkanPipeline(device, swapchain);
            pipeline.Init();

            commands = new VulkanCommands(device, swapchain, pipeline);
            commands.Init();

            CreateVertexBuffer();
            Console.WriteLine("[Vulkan] Backend initialized");
        }

        public void SetEventHandler(Action<string, Value[]> handler)
        {
            
        }

        private void AttachMetalLayer()
        {
            //attach metallayer to the view so mvk can use it (i hate VULKAN )
            var caMetalLayerClass = ObjC.GetClass("CAMetalLayer");
            var alloc = ObjC.RegisterName("alloc");
            var init = ObjC.RegisterName("init");
            var layer = ObjC.MsgSend(ObjC.MsgSend(caMetalLayerClass, alloc), init);

            var setWantsLayer = ObjC.RegisterName("setWantsLayer:");
            var setLayer = ObjC.RegisterName("setLayer:");
            ObjC.MsgSendVoid(window!.View, setWantsLayer, true);
            ObjC.MsgSendVoid(window!.View, setLayer, layer);
        }

        private void CreateSurface(out IntPtr surface)
        {
            var createInfo = new VkMacOSSurfaceCreateInfoMVK
            {
                SType = VkStructureType.VK_STRUCTURE_TYPE_MACOS_SURFACE_CREATE_INFO_MVK,
                PNext = IntPtr.Zero,
                Flags = 0,
                PView = window!.View
            };

            VK.Check(VK.vkCreateMacOSSurfaceMVK(device!.Instance, ref createInfo, IntPtr.Zero, out surface), "vkCreateMacOSSurfaceMVK");
            Console.WriteLine("[Vulkan] Surface created");
        }

        private void CreateVertexBuffer()
        {
            ulong bufferSize = MaxVertices * 6 * sizeof(float);

            var createInfo = new VkBufferCreateInfo
            {
                SType = VkStructureType.VK_STRUCTURE_TYPE_BUFFER_CREATE_INFO,
                PNext = IntPtr.Zero,
                Flags = 0,
                Size = bufferSize,
                Usage = (uint)VkBufferUsageFlagBits.VK_BUFFER_USAGE_VERTEX_BUFFER_BIT,
                SharingMode = VkSharingMode.VK_SHARING_MODE_EXCLUSIVE,
                QueueFamilyIndexCount = 0,
                PQueueFamilyIndices = null
            };

            VK.Check(VK.vkCreateBuffer(device!.Device, ref createInfo, IntPtr.Zero, out vertexBuffer), "vkCreateBuffer");

            //get memory requirements
            VK.vkGetBufferMemoryRequirements(device.Device, vertexBuffer, out var memReqs);

            //find host visible + coherent memory type
            VK.vkGetPhysicalDeviceMemoryProperties(device.PhysicalDevice, out var memProps);
            uint memTypeIndex = FindMemoryType(memReqs.MemoryTypeBits, memProps, (uint)(VkMemoryPropertyFlagBits.VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT | VkMemoryPropertyFlagBits.VK_MEMORY_PROPERTY_HOST_COHERENT_BIT));

            var allocInfo = new VkMemoryAllocateInfo
            {
                SType = VkStructureType.VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO,
                PNext = IntPtr.Zero,
                AllocationSize = memReqs.Size,
                MemoryTypeIndex = memTypeIndex
            };

            VK.Check(VK.vkAllocateMemory(device.Device, ref allocInfo, IntPtr.Zero, out vertexBufferMemory), "vkAllocateMemory");
            VK.Check(VK.vkBindBufferMemory(device.Device, vertexBuffer, vertexBufferMemory, 0), "vkBindBufferMemory");

            Console.WriteLine("[Vulkan] Vertex buffer created");
        }

        private static uint FindMemoryType(uint typeFilter, VkPhysicalDeviceMemoryProperties memProps, uint properties)
        {
            for (uint i = 0; i < memProps.MemoryTypeCount; i++)
            {
                if ((typeFilter & (1u << (int)i)) != 0 && (memProps.MemoryTypes[i].PropertyFlags & properties) == properties)
                    return i;
            }
            throw new Exception("Failed to find suitable memory type");
        }

        public void DrawRect(float x, float y, float w, float h, float r, float g, float b)
        {
            //Convert pixel coords to NDC
            //Convert pixel coords to NDC (-1 to 1)
            float ndcX = (x / (width  / 2f)) - 1f;
            float ndcY = (y / (height / 2f)) - 1f;  //remove the 1f - ... flip
            float ndcW = w / (width  / 2f);
            float ndcH = h / (height / 2f);

            //6 vertices — position (vec2) + color (vec4)
            float[] vertices = {
                ndcX, ndcY, r, g, b, 1f,  //top left
                ndcX + ndcW, ndcY, r, g, b, 1f,  //top right
                ndcX, ndcY + ndcH, r, g, b, 1f,  //bottom left
                ndcX + ndcW, ndcY, r, g, b, 1f,  //top right
                ndcX + ndcW, ndcY + ndcH, r, g, b, 1f,  //bottom right
                ndcX, ndcY + ndcH, r, g, b, 1f,  //bottom left
            };

            //upload to vertex buffer
            VK.Check(VK.vkMapMemory(device!.Device, vertexBufferMemory, 0, (ulong)(vertices.Length * sizeof(float)), 0, out var mapped), "vkMapMemory");

            fixed (float* pVertices = vertices)
                Buffer.MemoryCopy(pVertices, (void*)mapped, vertices.Length * sizeof(float), vertices.Length * sizeof(float));

            VK.vkUnmapMemory(device.Device, vertexBufferMemory);

            //wait for previous frame
            commands!.WaitAndReset();

            //get next swapchain image
            VK.Check(VK.vkAcquireNextImageKHR(device.Device, swapchain!.Swapchain, ulong.MaxValue, commands.ImageAvailable, IntPtr.Zero, out uint imageIndex), "vkAcquireNextImageKHR");

            //record and submit
            commands.RecordDraw(imageIndex, vertexBuffer, (uint)(vertices.Length / 6));
            commands.Submit();
            commands.Present(imageIndex);
        }

        public void DrawTriangle(float x1, float y1, float x2, float y2, float x3, float y3, float r, float g, float b)
        {
            Console.WriteLine(x1);
            Console.WriteLine(y1);
            Console.WriteLine(x2);
            Console.WriteLine(y2);
            Console.WriteLine(x3);
            Console.WriteLine(y3);
            //Convert pixel coords to NDC (-1 to 1)
            float ndcX1 = (x1 / width) - 1f ;
            float ndcY1 = (y1 / height) - 1f ;
            float ndcX2 = (x2 / width) - 1f ;
            float ndcY2 = (y2 / height) - 1f ;
            float ndcX3 = (x3 / width) - 1f ;
            float ndcY3 = (y3 / height) - 1f ;

            //6 vertices — position (vec2) + color (vec4)
            float[] vertices = {
                ndcX1, ndcY1, r, g, b, 1f,  //vertice1
                ndcX2, ndcY2, r, g, b, 1f,  //vertice2
                ndcX3, ndcY3, r, g, b, 1f,  //vertice3
            };

            //upload to vertex buffer
            VK.Check(VK.vkMapMemory(device!.Device, vertexBufferMemory, 0, (ulong)(vertices.Length * sizeof(float)), 0, out var mapped), "vkMapMemory");

            fixed (float* pVertices = vertices)
                Buffer.MemoryCopy(pVertices, (void*)mapped, vertices.Length * sizeof(float), vertices.Length * sizeof(float));

            VK.vkUnmapMemory(device.Device, vertexBufferMemory);

            //wait for previous frame
            commands!.WaitAndReset();

            //get next swapchain image
            VK.Check(VK.vkAcquireNextImageKHR(device.Device, swapchain!.Swapchain, ulong.MaxValue, commands.ImageAvailable, IntPtr.Zero, out uint imageIndex), "vkAcquireNextImageKHR");

            //record and submit
            commands.RecordDraw(imageIndex, vertexBuffer, (uint)(vertices.Length / 6));
            commands.Submit();
            commands.Present(imageIndex);
        }

        public void DrawCircle(float x, float y, float radius, float r, float g, float b)
        {
            float ndcX = (x / width) * 2f - 1f;
            float ndcY = (y / height) * 2f - 1f;
            float ndcRadiusX = (radius / width) * 2f;
            float ndcRadiusY = (radius / height) * 2f;

            float left   = ndcX - ndcRadiusX;
            float right  = ndcX + ndcRadiusX;
            float top    = ndcY + ndcRadiusY;
            float bottom = ndcY - ndcRadiusY;

            float[] vertices = {
                // Triangle 1
                left,  bottom, r, g, b, 1f,
                right, bottom, r, g, b, 1f,
                left,  top,    r, g, b, 1f,

                // Triangle 2
                left,  top,    r, g, b, 1f,
                right, bottom, r, g, b, 1f,
                right, top,    r, g, b, 1f,
            };

            //upload to vertex buffer
            VK.Check(VK.vkMapMemory(device!.Device, vertexBufferMemory, 0, (ulong)(vertices.Length * sizeof(float)), 0, out var mapped), "vkMapMemory");

            fixed (float* pVertices = vertices)
                Buffer.MemoryCopy(pVertices, (void*)mapped, vertices.Length * sizeof(float), vertices.Length * sizeof(float));

            VK.vkUnmapMemory(device.Device, vertexBufferMemory);

            //wait for previous frame
            commands!.WaitAndReset();

            //get next swapchain image
            VK.Check(VK.vkAcquireNextImageKHR(device.Device, swapchain!.Swapchain, ulong.MaxValue, commands.ImageAvailable, IntPtr.Zero, out uint imageIndex), "vkAcquireNextImageKHR");

            //record and submit
            commands.RecordDraw(imageIndex, vertexBuffer, (uint)(vertices.Length / 6), ndcX, ndcY, ndcRadiusX, radius);
            commands.Submit();
            commands.Present(imageIndex);
        }

        public void BeginFrame() { }
        public void EndFrame() { }

        public void RunLoop() => window!.RunLoop();

        public void Destroy()
        {
            VK.vkDeviceWaitIdle(device!.Device);
            commands?.Destroy();
            pipeline?.Destroy();
            swapchain?.Destroy();
            device?.Destroy();
        }

        public nint GetLayerPtr() => window!.Layer;
        public nint GetDevicePtr() => window!.Device;
    }
}