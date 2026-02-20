using System;
using System.Runtime.InteropServices;

namespace JSONScript.VM.Graphics.Vulkan
{
    public unsafe class VulkanCommands
    {
        public IntPtr CommandPool { get; private set; }
        public IntPtr CommandBuffer { get; private set; }
        public IntPtr ImageAvailable { get; private set; }
        public IntPtr RenderFinished { get; private set; }
        public IntPtr InFlight { get; private set; }

        private readonly VulkanDevice device;
        private readonly VulkanSwapchain swapchain;
        private readonly VulkanPipeline pipeline;

        public VulkanCommands(VulkanDevice device, VulkanSwapchain swapchain, VulkanPipeline pipeline)
        {
            this.device = device;
            this.swapchain = swapchain;
            this.pipeline = pipeline;
        }

        public void Init()
        {
            CreateCommandPool();
            AllocateCommandBuffer();
            CreateSyncObjects();
            Console.WriteLine("[Vulkan] Commands and sync objects created");
        }

        private void CreateCommandPool()
        {
            var createInfo = new VkCommandPoolCreateInfo
            {
                SType = VkStructureType.VK_STRUCTURE_TYPE_COMMAND_POOL_CREATE_INFO,
                PNext = IntPtr.Zero,
                Flags = 0x00000002, // VK_COMMAND_POOL_CREATE_RESET_COMMAND_BUFFER_BIT
                QueueFamilyIndex = device.GraphicsFamily
            };

            VK.Check(VK.vkCreateCommandPool(device.Device, ref createInfo, IntPtr.Zero, out var pool), "vkCreateCommandPool");
            CommandPool = pool;
        }

        private void AllocateCommandBuffer()
        {
            IntPtr cmdBuf;
            var allocInfo = new VkCommandBufferAllocateInfo
            {
                SType = VkStructureType.VK_STRUCTURE_TYPE_COMMAND_BUFFER_ALLOCATE_INFO,
                PNext = IntPtr.Zero,
                CommandPool = CommandPool,
                Level = VkCommandBufferLevel.VK_COMMAND_BUFFER_LEVEL_PRIMARY,
                CommandBufferCount = 1
            };

            VK.Check(VK.vkAllocateCommandBuffers(device.Device, ref allocInfo, &cmdBuf), "vkAllocateCommandBuffers");
            CommandBuffer = cmdBuf;
        }

        private void CreateSyncObjects()
        {
            var semaphoreInfo = new VkSemaphoreCreateInfo
            {
                SType = VkStructureType.VK_STRUCTURE_TYPE_SEMAPHORE_CREATE_INFO,
                PNext = IntPtr.Zero,
                Flags = 0
            };

            var fenceInfo = new VkFenceCreateInfo
            {
                SType = VkStructureType.VK_STRUCTURE_TYPE_FENCE_CREATE_INFO,
                PNext = IntPtr.Zero,
                Flags = VkFenceCreateFlagBits.VK_FENCE_CREATE_SIGNALED_BIT
            };

            VK.Check(VK.vkCreateSemaphore(device.Device, ref semaphoreInfo, IntPtr.Zero, out var ia), "vkCreateSemaphore(ImageAvailable)");
            VK.Check(VK.vkCreateSemaphore(device.Device, ref semaphoreInfo, IntPtr.Zero, out var rf), "vkCreateSemaphore(RenderFinished)");
            VK.Check(VK.vkCreateFence(device.Device, ref fenceInfo, IntPtr.Zero, out var inf), "vkCreateFence");

            ImageAvailable = ia;
            RenderFinished = rf;
            InFlight = inf;
        }

        public void RecordDraw(uint imageIndex, IntPtr vertexBuffer, uint vertexCount)
        {
            // Reset and begin command buffer
            VK.Check(VK.vkResetCommandBuffer(CommandBuffer, 0), "vkResetCommandBuffer");

            var beginInfo = new VkCommandBufferBeginInfo
            {
                SType = VkStructureType.VK_STRUCTURE_TYPE_COMMAND_BUFFER_BEGIN_INFO,
                PNext = IntPtr.Zero,
                Flags = 0,
                PInheritanceInfo = IntPtr.Zero
            };

            VK.Check(VK.vkBeginCommandBuffer(CommandBuffer, ref beginInfo), "vkBeginCommandBuffer");

            // Begin render pass
            var clearColor = new VkClearValue
            {
                Color = new VkClearColorValue { R = 0f, G = 0f, B = 0f, A = 1f }
            };

            VkClearValue* pClearColor = &clearColor;

            var renderPassInfo = new VkRenderPassBeginInfo
            {
                SType = VkStructureType.VK_STRUCTURE_TYPE_RENDER_PASS_BEGIN_INFO,
                PNext = IntPtr.Zero,
                RenderPass = pipeline.RenderPass,
                Framebuffer = pipeline.Framebuffers[imageIndex],
                RenderArea = new VkRect2D
                {
                    Offset = new VkOffset2D { X = 0, Y = 0 },
                    Extent = swapchain.Extent
                },
                ClearValueCount = 1,
                PClearValues = pClearColor
            };

            VK.vkCmdBeginRenderPass(CommandBuffer, ref renderPassInfo, VkSubpassContents.VK_SUBPASS_CONTENTS_INLINE);

            // Bind pipeline
            VK.vkCmdBindPipeline(CommandBuffer, VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_GRAPHICS, pipeline.Pipeline);

            // Set dynamic viewport
            var viewport = new VkViewport
            {
                X = 0f,
                Y = 0f,
                Width = swapchain.Extent.Width,
                Height = swapchain.Extent.Height,
                MinDepth = 0f,
                MaxDepth = 1f
            };
            VK.vkCmdSetViewport(CommandBuffer, 0, 1, ref viewport);

            // Set dynamic scissor
            var scissor = new VkRect2D
            {
                Offset = new VkOffset2D { X = 0, Y = 0 },
                Extent = swapchain.Extent
            };
            VK.vkCmdSetScissor(CommandBuffer, 0, 1, ref scissor);

            // Bind vertex buffer
            ulong offset = 0;
            VK.vkCmdBindVertexBuffers(CommandBuffer, 0, 1, ref vertexBuffer, ref offset);

            // Draw
            VK.vkCmdDraw(CommandBuffer, vertexCount, 1, 0, 0);

            // End render pass and command buffer
            VK.vkCmdEndRenderPass(CommandBuffer);
            VK.Check(VK.vkEndCommandBuffer(CommandBuffer), "vkEndCommandBuffer");
        }

        public void Submit()
        {
            IntPtr waitSem = ImageAvailable;
            IntPtr signalSem = RenderFinished;
            IntPtr cmdBuf = CommandBuffer;
            int waitStage = (int)VkPipelineStageFlagBits.VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT;

            IntPtr* pWaitSem = &waitSem;
            IntPtr* pSignalSem = &signalSem;
            IntPtr* pCmdBuf = &cmdBuf;
            int* pWaitStage = &waitStage;

            var submitInfo = new VkSubmitInfo
            {
                SType = VkStructureType.VK_STRUCTURE_TYPE_SUBMIT_INFO,
                PNext = IntPtr.Zero,
                WaitSemaphoreCount = 1,
                PWaitSemaphores = pWaitSem,
                PWaitDstStageMask = pWaitStage,
                CommandBufferCount = 1,
                PCommandBuffers = pCmdBuf,
                SignalSemaphoreCount = 1,
                PSignalSemaphores = pSignalSem
            };

            VK.Check(VK.vkQueueSubmit(device.GraphicsQueue, 1, ref submitInfo, InFlight), "vkQueueSubmit");
        }

        public void Present(uint imageIndex)
        {
            IntPtr swapchainHandle = swapchain.Swapchain;
            IntPtr waitSem = RenderFinished;
            uint idx = imageIndex;

            IntPtr* pSwapchain = &swapchainHandle;
            IntPtr* pWaitSem = &waitSem;
            uint* pIdx = &idx;

            var presentInfo = new VkPresentInfoKHR
            {
                SType = VkStructureType.VK_STRUCTURE_TYPE_PRESENT_INFO_KHR,
                PNext = IntPtr.Zero,
                WaitSemaphoreCount = 1,
                PWaitSemaphores = pWaitSem,
                SwapchainCount = 1,
                PSwapchains = pSwapchain,
                PImageIndices = pIdx,
                PResults = null
            };

            VK.Check(VK.vkQueuePresentKHR(device.PresentQueue, ref presentInfo), "vkQueuePresentKHR");
        }

        public void WaitAndReset()
        {
            IntPtr fence = InFlight;
            VK.vkWaitForFences(device.Device, 1, ref fence, 1, ulong.MaxValue);
            VK.vkResetFences(device.Device, 1, ref fence);
        }

        public void Destroy()
        {
            VK.vkDestroyDevice(device.Device, IntPtr.Zero);
        }
    }
}