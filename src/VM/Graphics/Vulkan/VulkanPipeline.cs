using System;
using System.IO;
using System.Runtime.InteropServices;

namespace JSONScript.VM.Graphics.Vulkan
{
    public unsafe class VulkanPipeline
    {
        public IntPtr RenderPass{ get ; private set; }
        public IntPtr Pipeline { get; private set; }
        public IntPtr PipelineLayout { get; private set; }
        public IntPtr[] Framebuffers { get; private set; } = Array.Empty<IntPtr>();

        private readonly VulkanDevice device;
        private readonly VulkanSwapchain swapchain;

        public VulkanPipeline(VulkanDevice device, VulkanSwapchain swapchain)
        {
            this.device = device;
            this.swapchain = swapchain;
        }

        public void Init()
        {
            CreateRenderPass();
            CreatePipelineLayout();
            CreateGraphicsPipeline();
            CreateFramebuffers();
            Console.WriteLine("[Vulkan] Pipeline created");
        }

        private void CreateRenderPass()
        {
            var colorAttachment = new VkAttachmentDescription
            {
                Flags = 0,
                Format = swapchain.Format,
                Samples = VkSampleCountFlagBits.VK_SAMPLE_COUNT_1_BIT,
                LoadOp = VkAttachmentLoadOp.VK_ATTACHMENT_LOAD_OP_CLEAR,
                StoreOp = VkAttachmentStoreOp.VK_ATTACHMENT_STORE_OP_STORE,
                StencilLoadOp = VkAttachmentLoadOp.VK_ATTACHMENT_LOAD_OP_DONT_CARE,
                StencilStoreOp = VkAttachmentStoreOp.VK_ATTACHMENT_STORE_OP_DONT_CARE,
                InitialLayout = VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED,
                FinalLayout = VkImageLayout.VK_IMAGE_LAYOUT_PRESENT_SRC_KHR
            };

            var colorRef = new VkAttachmentReference
            {
                Attachment = 0,
                Layout = VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL
            };

            VkAttachmentDescription* pAttachment = &colorAttachment;
            VkAttachmentReference* pColorRef = &colorRef;

            var subpass = new VkSubpassDescription
            {
                Flags = 0,
                PipelineBindPoint = VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_GRAPHICS,
                InputAttachmentCount = 0,
                PInputAttachments = null,
                ColorAttachmentCount = 1,
                PColorAttachments = pColorRef,
                PResolveAttachments = IntPtr.Zero,
                PDepthStencilAttachment = IntPtr.Zero,
                PreserveAttachmentCount = 0,
                PPreserveAttachments = null
            };

            VkSubpassDescription* pSubpass = &subpass;

            var createInfo = new VkRenderPassCreateInfo
            {
                SType = VkStructureType.VK_STRUCTURE_TYPE_RENDER_PASS_CREATE_INFO,
                PNext = IntPtr.Zero,
                Flags = 0,
                AttachmentCount = 1,
                PAttachments = pAttachment,
                SubpassCount = 1,
                PSubpasses = pSubpass,
                DependencyCount = 0,
                PDependencies = IntPtr.Zero
            };

            VK.Check(VK.vkCreateRenderPass(device.Device, ref createInfo, IntPtr.Zero, out var rp), "vkCreateRenderPass");
            RenderPass = rp;
        }

        private void CreatePipelineLayout()
        {
            var createInfo = new VkPipelineLayoutCreateInfo
            {
                SType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_LAYOUT_CREATE_INFO,
                PNext = IntPtr.Zero,
                Flags = 0,
                SetLayoutCount = 0,
                PSetLayouts = IntPtr.Zero,
                PushConstantRangeCount = 0,
                PPushConstantRanges = IntPtr.Zero
            };

            VK.Check(VK.vkCreatePipelineLayout(device.Device, ref createInfo, IntPtr.Zero, out var layout), "vkCreatePipelineLayout");
            PipelineLayout = layout;
        }

        private void CreateGraphicsPipeline()
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            byte[] vertCode = File.ReadAllBytes(Path.Combine(basePath, "Shaders", "vert.spv"));
            byte[] fragCode = File.ReadAllBytes(Path.Combine(basePath, "Shaders", "frag.spv"));

            IntPtr vertModule = CreateShaderModule(vertCode);
            IntPtr fragModule = CreateShaderModule(fragCode);

            var entryPoint = Marshal.StringToHGlobalAnsi("main");

            var shaderStages = new VkPipelineShaderStageCreateInfo[2];
            shaderStages[0] = new VkPipelineShaderStageCreateInfo
            {
                SType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,
                PNext = IntPtr.Zero,
                Flags = 0,
                Stage = VkShaderStageFlagBits.VK_SHADER_STAGE_VERTEX_BIT,
                Module = vertModule,
                PName = entryPoint,
                PSpecializationInfo = IntPtr.Zero
            };
            shaderStages[1] = new VkPipelineShaderStageCreateInfo
            {
                SType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,
                PNext = IntPtr.Zero,
                Flags = 0,
                Stage = VkShaderStageFlagBits.VK_SHADER_STAGE_FRAGMENT_BIT,
                Module = fragModule,
                PName = entryPoint,
                PSpecializationInfo = IntPtr.Zero
            };

            var bindingDesc = new VkVertexInputBindingDescription
            {
                Binding = 0,
                Stride = 6 * sizeof(float),
                InputRate = VkVertexInputRate.VK_VERTEX_INPUT_RATE_VERTEX
            };

            var attrDescs = new VkVertexInputAttributeDescription[2];
            attrDescs[0] = new VkVertexInputAttributeDescription
            {
                Location = 0,
                Binding = 0,
                Format = VkFormat.VK_FORMAT_R32G32_SFLOAT,
                Offset = 0
            };
            attrDescs[1] = new VkVertexInputAttributeDescription
            {
                Location = 1,
                Binding = 0,
                Format = VkFormat.VK_FORMAT_R32G32B32A32_SFLOAT,
                Offset = 2 * sizeof(float)
            };

            var dynamicStates = new VkDynamicState[]
            {
                VkDynamicState.VK_DYNAMIC_STATE_VIEWPORT,
                VkDynamicState.VK_DYNAMIC_STATE_SCISSOR
            };

            var colorBlendAttachment = new VkPipelineColorBlendAttachmentState
            {
                BlendEnable = 0,
                SrcColorBlendFactor = VkBlendFactor.VK_BLEND_FACTOR_ONE,
                DstColorBlendFactor = VkBlendFactor.VK_BLEND_FACTOR_ZERO,
                ColorBlendOp = VkBlendOp.VK_BLEND_OP_ADD,
                SrcAlphaBlendFactor = VkBlendFactor.VK_BLEND_FACTOR_ONE,
                DstAlphaBlendFactor = VkBlendFactor.VK_BLEND_FACTOR_ZERO,
                AlphaBlendOp = VkBlendOp.VK_BLEND_OP_ADD,
                ColorWriteMask = 0xF
            };

            VkVertexInputBindingDescription* pBindingDesc = &bindingDesc;
            VkPipelineColorBlendAttachmentState* pColorBlend = &colorBlendAttachment;

            fixed (VkPipelineShaderStageCreateInfo*   pStages = shaderStages)
            fixed (VkVertexInputAttributeDescription* pAttrDescs = attrDescs)
            fixed (VkDynamicState* pDynamicStates = dynamicStates)
            {
                var vertexInput = new VkPipelineVertexInputStateCreateInfo
                {
                    SType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_VERTEX_INPUT_STATE_CREATE_INFO,
                    PNext = IntPtr.Zero,
                    Flags = 0,
                    VertexBindingDescriptionCount = 1,
                    PVertexBindingDescriptions = pBindingDesc,
                    VertexAttributeDescriptionCount = 2,
                    PVertexAttributeDescriptions = pAttrDescs
                };

                var inputAssembly = new VkPipelineInputAssemblyStateCreateInfo
                {
                    SType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_INPUT_ASSEMBLY_STATE_CREATE_INFO,
                    PNext = IntPtr.Zero,
                    Flags = 0,
                    Topology = VkPrimitiveTopology.VK_PRIMITIVE_TOPOLOGY_TRIANGLE_LIST,
                    PrimitiveRestartEnable = 0
                };

                var viewportState = new VkPipelineViewportStateCreateInfo
                {
                    SType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_VIEWPORT_STATE_CREATE_INFO,
                    PNext = IntPtr.Zero,
                    Flags = 0,
                    ViewportCount = 1,
                    PViewports = null,
                    ScissorCount = 1,
                    PScissors = null
                };

                var rasterizer = new VkPipelineRasterizationStateCreateInfo
                {
                    SType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_RASTERIZATION_STATE_CREATE_INFO,
                    PNext = IntPtr.Zero,
                    Flags = 0,
                    DepthClampEnable = 0,
                    RasterizerDiscardEnable = 0,
                    PolygonMode  = VkPolygonMode.VK_POLYGON_MODE_FILL,
                    CullMode = (uint)VkCullModeFlagBits.VK_CULL_MODE_NONE,
                    FrontFace = VkFrontFace.VK_FRONT_FACE_CLOCKWISE,
                    DepthBiasEnable = 0,
                    DepthBiasConstantFactor = 0,
                    DepthBiasClamp = 0,
                    DepthBiasSlopeFactor = 0,
                    LineWidth = 1.0f
                };

                var multisampling = new VkPipelineMultisampleStateCreateInfo
                {
                    SType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_MULTISAMPLE_STATE_CREATE_INFO,
                    PNext = IntPtr.Zero,
                    Flags = 0,
                    RasterizationSamples = VkSampleCountFlagBits.VK_SAMPLE_COUNT_1_BIT,
                    SampleShadingEnable = 0,
                    MinSampleShading = 1.0f,
                    PSampleMask = IntPtr.Zero,
                    AlphaToCoverageEnable = 0,
                    AlphaToOneEnable = 0
                };

                var colorBlending = new VkPipelineColorBlendStateCreateInfo
                {
                    SType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_COLOR_BLEND_STATE_CREATE_INFO,
                    PNext = IntPtr.Zero,
                    Flags = 0,
                    LogicOpEnable = 0,
                    LogicOp = VkLogicOp.VK_LOGIC_OP_COPY,
                    AttachmentCount = 1,
                    PAttachments = pColorBlend
                };

                var dynamicState = new VkPipelineDynamicStateCreateInfo
                {
                    SType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_DYNAMIC_STATE_CREATE_INFO,
                    PNext = IntPtr.Zero,
                    Flags = 0,
                    DynamicStateCount = (uint)dynamicStates.Length,
                    PDynamicStates = pDynamicStates
                };

                // Take addresses of locals — valid since we're in unsafe context
                VkPipelineVertexInputStateCreateInfo* pVI = &vertexInput;
                VkPipelineInputAssemblyStateCreateInfo* pIA  = &inputAssembly;
                VkPipelineViewportStateCreateInfo* pVP = &viewportState;
                VkPipelineRasterizationStateCreateInfo* pRS = &rasterizer;
                VkPipelineMultisampleStateCreateInfo* pMS = &multisampling;
                VkPipelineColorBlendStateCreateInfo* pCB = &colorBlending;
                VkPipelineDynamicStateCreateInfo* pDS = &dynamicState;

                var pipelineInfo = new VkGraphicsPipelineCreateInfo
                {
                    SType = VkStructureType.VK_STRUCTURE_TYPE_GRAPHICS_PIPELINE_CREATE_INFO,
                    PNext = IntPtr.Zero,
                    Flags = 0,
                    StageCount = 2,
                    PStages = pStages,
                    PVertexInputState = pVI,
                    PInputAssemblyState = pIA,
                    PTessellationState = IntPtr.Zero,
                    PViewportState = pVP,
                    PRasterizationState = pRS,
                    PMultisampleState = pMS,
                    PDepthStencilState = IntPtr.Zero,
                    PColorBlendState = pCB,
                    PDynamicState = pDS,
                    Layout = PipelineLayout,
                    RenderPass = RenderPass,
                    Subpass = 0,
                    BasePipelineHandle  = IntPtr.Zero,
                    BasePipelineIndex = -1
                };

                VK.Check(VK.vkCreateGraphicsPipelines( device.Device, IntPtr.Zero, 1, ref pipelineInfo, IntPtr.Zero, out var pipeline), "vkCreateGraphicsPipelines");
                Pipeline = pipeline;
            }

            Marshal.FreeHGlobal(entryPoint);
        }

        private IntPtr CreateShaderModule(byte[] code)
        {
            fixed (byte* pCode = code)
            {
                var createInfo = new VkShaderModuleCreateInfo
                {
                    SType = VkStructureType.VK_STRUCTURE_TYPE_SHADER_MODULE_CREATE_INFO,
                    PNext = IntPtr.Zero,
                    Flags = 0,
                    CodeSize = (UIntPtr)code.Length,
                    PCode = (uint*)pCode
                };

                VK.Check(VK.vkCreateShaderModule(device.Device, ref createInfo, IntPtr.Zero, out var module), "vkCreateShaderModule");
                return module;
            }
        }

        private void CreateFramebuffers()
        {
            Framebuffers = new IntPtr[swapchain.ImageViews.Length];

            for (int i = 0; i < swapchain.ImageViews.Length; i++)
            {
                fixed (IntPtr* pAttachment = &swapchain.ImageViews[i])
                {
                    var createInfo = new VkFramebufferCreateInfo
                    {
                        SType = VkStructureType.VK_STRUCTURE_TYPE_FRAMEBUFFER_CREATE_INFO,
                        PNext = IntPtr.Zero,
                        Flags = 0,
                        RenderPass = RenderPass,
                        AttachmentCount = 1,
                        PAttachments = pAttachment,
                        Width = swapchain.Extent.Width,
                        Height = swapchain.Extent.Height,
                        Layers = 1
                    };

                    VK.Check(VK.vkCreateFramebuffer(device.Device, ref createInfo, IntPtr.Zero, out Framebuffers[i]), $"vkCreateFramebuffer[{i}]");
                }
            }
        }

        public void Destroy()
        {
            foreach (var fb in Framebuffers)
                VK.vkDestroySwapchainKHR(device.Device, fb, IntPtr.Zero);
        }
    }
}