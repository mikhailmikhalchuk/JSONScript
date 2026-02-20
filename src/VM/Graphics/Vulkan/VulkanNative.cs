using System;
using System.Runtime.InteropServices;

namespace JSONScript.VM.Graphics.Vulkan
{
    // Result codes
    public enum VkResult : int
    {
        /// <summary>
        /// Command successfully completed.
        /// </summary>
        VK_SUCCESS = 0,
        /// <summary>
        /// A fence or query has not yet completed.
        /// </summary>
        VK_NOT_READY = 1,
        /// <summary>
        /// A wait operation has not completed in the specified time.
        /// </summary>
        VK_TIMEOUT = 2,
        /// <summary>
        /// An event is signaled.
        /// </summary>
        VK_EVENT_SET = 3,
        /// <summary>
        /// An event is unsignaled.
        /// </summary>
        VK_EVENT_RESET = 4,
        /// <summary>
        /// A return array was too small for the result.
        /// </summary>
        VK_INCOMPLETE = 5,
        /// <summary>
        /// A host memory allocation has failed.
        /// </summary>
        VK_ERROR_OUT_OF_HOST_MEMORY = -1,
        /// <summary>
        /// A device memory allocation has failed.
        /// </summary>
        VK_ERROR_OUT_OF_DEVICE_MEMORY = -2,
        /// <summary>
        /// Initialization of an object could not be completed for implementation-specific reasons.
        /// </summary>
        VK_ERROR_INITIALIZATION_FAILED = -3,
        /// <summary>
        /// The logical or physical device has been lost. See <see href="https://docs.vulkan.org/spec/latest/chapters/devsandqueues.html#devsandqueues-lost-device">Lost Device</see>.
        /// </summary>
        VK_ERROR_DEVICE_LOST = -4,
        /// <summary>
        /// Mapping of a memory object has failed.
        /// </summary>
        VK_ERROR_MEMORY_MAP_FAILED = -5,
        /// <summary>
        /// A requested layer is not present or could not be loaded.
        /// </summary>
        VK_ERROR_LAYER_NOT_PRESENT = -6,
        /// <summary>
        /// A requested extension is not supported.
        /// </summary>
        VK_ERROR_EXTENSION_NOT_PRESENT = -7,
        /// <summary>
        /// A requested feature is not supported.
        /// </summary>
        VK_ERROR_FEATURE_NOT_PRESENT = -8,
        /// <summary>
        /// The requested version of Vulkan is not supported by the driver or is otherwise incompatible for implementation-specific reasons.
        /// </summary>
        VK_ERROR_INCOMPATIBLE_DRIVER = -9,
        /// <summary>
        /// A surface is no longer available.
        /// </summary>
        VK_ERROR_SURFACE_LOST_KHR = -1000000000,
        /// <summary>
        /// A surface has changed in such a way that it is no longer compatible with the swapchain, and further presentation requests using the swapchain will fail. Applications must query the new surface properties and recreate their swapchain if they wish to continue presenting to the surface.
        /// </summary>
        VK_ERROR_OUT_OF_DATE_KHR = -1000001004,
        VK_SUBOPTIMAL_KHR = 1000001003,
    }

    //https://docs.vulkan.org/spec/latest/chapters/fundamentals.html#VkStructureType
    //Im not doing allat
    public enum VkStructureType : int
    {
        VK_STRUCTURE_TYPE_APPLICATION_INFO = 0,
        VK_STRUCTURE_TYPE_INSTANCE_CREATE_INFO = 1,
        VK_STRUCTURE_TYPE_DEVICE_QUEUE_CREATE_INFO = 2,
        VK_STRUCTURE_TYPE_DEVICE_CREATE_INFO = 3,
        VK_STRUCTURE_TYPE_SUBMIT_INFO = 4,
        VK_STRUCTURE_TYPE_RENDER_PASS_BEGIN_INFO = 43,
        VK_STRUCTURE_TYPE_COMMAND_POOL_CREATE_INFO = 39,
        VK_STRUCTURE_TYPE_COMMAND_BUFFER_ALLOCATE_INFO = 40,
        VK_STRUCTURE_TYPE_COMMAND_BUFFER_BEGIN_INFO = 42,
        VK_STRUCTURE_TYPE_RENDER_PASS_CREATE_INFO = 38,
        VK_STRUCTURE_TYPE_FRAMEBUFFER_CREATE_INFO = 37,
        VK_STRUCTURE_TYPE_SHADER_MODULE_CREATE_INFO = 16,
        VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO = 18,
        VK_STRUCTURE_TYPE_PIPELINE_VERTEX_INPUT_STATE_CREATE_INFO = 19,
        VK_STRUCTURE_TYPE_PIPELINE_INPUT_ASSEMBLY_STATE_CREATE_INFO = 20,
        VK_STRUCTURE_TYPE_PIPELINE_VIEWPORT_STATE_CREATE_INFO = 22,
        VK_STRUCTURE_TYPE_PIPELINE_RASTERIZATION_STATE_CREATE_INFO = 23,
        VK_STRUCTURE_TYPE_PIPELINE_MULTISAMPLE_STATE_CREATE_INFO = 24,
        VK_STRUCTURE_TYPE_PIPELINE_COLOR_BLEND_STATE_CREATE_INFO = 26,
        VK_STRUCTURE_TYPE_GRAPHICS_PIPELINE_CREATE_INFO = 28,
        VK_STRUCTURE_TYPE_PIPELINE_LAYOUT_CREATE_INFO = 30,
        VK_STRUCTURE_TYPE_SWAPCHAIN_CREATE_INFO_KHR = 1000001000,
        VK_STRUCTURE_TYPE_PRESENT_INFO_KHR = 1000001001,
        VK_STRUCTURE_TYPE_IMAGE_VIEW_CREATE_INFO = 15,
        VK_STRUCTURE_TYPE_SEMAPHORE_CREATE_INFO = 9,
        VK_STRUCTURE_TYPE_FENCE_CREATE_INFO = 8,
        VK_STRUCTURE_TYPE_MACOS_SURFACE_CREATE_INFO_MVK = 1000123000,
        VK_STRUCTURE_TYPE_PIPELINE_DYNAMIC_STATE_CREATE_INFO = 27,
        VK_STRUCTURE_TYPE_BUFFER_CREATE_INFO = 12,
        VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO = 5,
    }

    public enum VkFormat : int
    {
        /// <summary>
        /// Specifies that the format is not specified.
        /// </summary>
        VK_FORMAT_UNDEFINED = 0,
        /// <summary>
        /// Specifies a four-component, 32-bit unsigned normalized format that has an 8-bit B component in byte 0, an 8-bit G component in byte 1, an 8-bit R component in byte 2, and an 8-bit A component in byte 3.
        /// </summary>
        VK_FORMAT_B8G8R8A8_UNORM = 44,
        /// <summary>
        /// Specifies a four-component, 32-bit unsigned normalized format that has an 8-bit B component stored with sRGB nonlinear encoding in byte 0, an 8-bit G component stored with sRGB nonlinear encoding in byte 1, an 8-bit R component stored with sRGB nonlinear encoding in byte 2, and an 8-bit A component in byte 3.
        /// </summary>
        VK_FORMAT_B8G8R8A8_SRGB = 50,
        /// <summary>
        /// Specifies a two-component, 64-bit signed floating-point format that has a 32-bit R component in bytes 0..3, and a 32-bit G component in bytes 4..7.
        /// </summary>
        VK_FORMAT_R32G32_SFLOAT = 103,
        /// <summary>
        /// Specifies a four-component, 128-bit signed floating-point format that has a 32-bit R component in bytes 0..3, a 32-bit G component in bytes 4..7, a 32-bit B component in bytes 8..11, and a 32-bit A component in bytes 12..15.
        /// </summary>
        VK_FORMAT_R32G32B32A32_SFLOAT = 109,
    }

    public enum VkColorSpaceKHR : int
    {
        /// <summary>
        /// Specifies support for the images in sRGB color space, encoded according to the sRGB specification.
        /// </summary>
        VK_COLOR_SPACE_SRGB_NONLINEAR_KHR = 0,
    }

    public enum VkPresentModeKHR : int
    {
        /// <summary>
        /// Specifies that the presentation engine does not wait for a vertical blanking period to update the current image, meaning this mode may result in visible tearing. No internal queuing of presentation requests is needed, as the requests are applied immediately.
        /// </summary>
        VK_PRESENT_MODE_IMMEDIATE_KHR = 0,
        /// <summary>
        /// Specifies that the presentation engine waits for the next vertical blanking period to update the current image. Tearing cannot be observed. An internal single-entry queue is used to hold pending presentation requests. If the queue is full when a new presentation request is received, the new request replaces the existing entry, and any images associated with the prior entry become available for reuse by the application. One request is removed from the queue and processed during each vertical blanking period in which the queue is non-empty.
        /// </summary>
        VK_PRESENT_MODE_MAILBOX_KHR = 1,
        /// <summary>
        /// Specifies that the presentation engine waits for the next vertical blanking period to update the current image. Tearing cannot be observed. An internal queue is used to hold pending presentation requests. New requests are appended to the end of the queue, and one request is removed from the beginning of the queue and processed during each vertical blanking period in which the queue is non-empty. This is the only value of <c>presentMode</c> that is required to be supported.
        /// </summary>
        VK_PRESENT_MODE_FIFO_KHR = 2,
    }

    public enum VkImageLayout : int
    {
        /// <summary>
        /// Specifies that the layout is unknown. Image memory cannot be transitioned into this layout. This layout can be used as the <c>initialLayout</c> member of VkImageCreateInfo. This layout can be used in place of the current image layout in a layout transition, but doing so will cause the contents of the image’s memory to be undefined.
        /// </summary>
        VK_IMAGE_LAYOUT_UNDEFINED = 0,
        /// <summary>
        /// Must only be used for presenting a presentable image for display.
        /// </summary>
        VK_IMAGE_LAYOUT_PRESENT_SRC_KHR = 1000001002,
        /// <summary>
        /// Must only be used as a color or resolve attachment in a <c>VkFramebuffer</c>. This layout is valid only for image subresources of images created with the <see cref="VkImageUsageFlagBits.VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT"/> usage flag set.
        /// </summary>
        VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL = 2,
    }

    public enum VkAttachmentLoadOp : int
    {
        /// <summary>
        /// Specifies that the previous contents of the image within the render area will be preserved as the initial values. For attachments with a depth/stencil format, this uses the access type VK_ACCESS_DEPTH_STENCIL_ATTACHMENT_READ_BIT. For attachments with a color format, this uses the access type VK_ACCESS_COLOR_ATTACHMENT_READ_BIT.
        /// </summary>
        VK_ATTACHMENT_LOAD_OP_LOAD = 0,
        /// <summary>
        /// Specifies that the contents within the render area will be cleared to a uniform value, which is specified when a render pass instance is begun. For attachments with a depth/stencil format, this uses the access type VK_ACCESS_DEPTH_STENCIL_ATTACHMENT_WRITE_BIT. For attachments with a color format, this uses the access type VK_ACCESS_COLOR_ATTACHMENT_WRITE_BIT.
        /// </summary>
        VK_ATTACHMENT_LOAD_OP_CLEAR = 1,
        /// <summary>
        /// Specifies that the previous contents within the area need not be preserved; the contents of the attachment will be <b>undefined</b> inside the render area. For attachments with a depth/stencil format, this uses the access type VK_ACCESS_DEPTH_STENCIL_ATTACHMENT_WRITE_BIT. For attachments with a color format, this uses the access type VK_ACCESS_COLOR_ATTACHMENT_WRITE_BIT.
        /// </summary>
        VK_ATTACHMENT_LOAD_OP_DONT_CARE = 2,
    }

    public enum VkAttachmentStoreOp : int
    {
        /// <summary>
        /// Specifies the contents generated during the render pass and within the render area are written to memory. For attachments with a depth/stencil format, this uses the access type VK_ACCESS_DEPTH_STENCIL_ATTACHMENT_WRITE_BIT. For attachments with a color format, this uses the access type VK_ACCESS_COLOR_ATTACHMENT_WRITE_BIT.
        /// </summary>
        VK_ATTACHMENT_STORE_OP_STORE = 0,
        /// <summary>
        /// Specifies the contents within the render area are not needed after rendering, and <b>may</b> be discarded; the contents of the attachment will be <b>undefined</b> inside the render area. For attachments with a depth/stencil format, this uses the access type VK_ACCESS_DEPTH_STENCIL_ATTACHMENT_WRITE_BIT. For attachments with a color format, this uses the access type VK_ACCESS_COLOR_ATTACHMENT_WRITE_BIT.
        /// </summary>
        VK_ATTACHMENT_STORE_OP_DONT_CARE = 1,
    }

    public enum VkPipelineBindPoint : int
    {
        /// <summary>
        /// Specifies binding as a compute pipeline.
        /// </summary>
        VK_PIPELINE_BIND_POINT_GRAPHICS = 0,
    }

    public enum VkShaderStageFlagBits : int
    {
        /// <summary>
        /// Specifies the vertex stage.
        /// </summary>
        VK_SHADER_STAGE_VERTEX_BIT = 0x00000001,
        /// <summary>
        /// Specifies the fragment stage.
        /// </summary>
        VK_SHADER_STAGE_FRAGMENT_BIT = 0x00000010,
    }

    public enum VkPrimitiveTopology : int
    {
        /// <summary>
        /// Specifies a series of <see href="https://docs.vulkan.org/spec/latest/chapters/drawing.html#drawing-triangle-lists">separate triangle primitives</see>.
        /// </summary>
        VK_PRIMITIVE_TOPOLOGY_TRIANGLE_LIST = 3,
    }

    public enum VkPolygonMode : int
    {
        /// <summary>
        /// Specifies that polygons are rendered using the polygon rasterization rules in <see href="https://docs.vulkan.org/spec/latest/chapters/primsrast.html#primsrast-polygons-basic">this section</see>.
        /// </summary>
        VK_POLYGON_MODE_FILL = 0,
    }

    public enum VkCullModeFlagBits : int
    {
        /// <summary>
        /// Specifies that no triangles are discarded.
        /// </summary>
        VK_CULL_MODE_NONE = 0,
    }

    public enum VkFrontFace : int
    {
        /// <summary>
        /// Specifies that a triangle with negative area is considered front-facing.
        /// </summary>
        VK_FRONT_FACE_CLOCKWISE = 1,
    }

    public enum VkSampleCountFlagBits : int
    {
        /// <summary>
        /// Specifies an image with one sample per pixel.
        /// </summary>
        VK_SAMPLE_COUNT_1_BIT = 1,
    }

    public enum VkBlendFactor : int
    {
        /// <summary>
        /// Specifies a blend factor of 0.<br/><br/>
        /// For RGB:
        /// <code>
        /// (0,0,0)
        /// </code><br/><br/>
        /// For alpha:<br/><br/>
        /// 0
        /// </summary>
        VK_BLEND_FACTOR_ZERO = 0,
        /// <summary>
        /// Specifies a blend factor of 1.<br/><br/>
        /// For RGB:
        /// <code>
        /// (1,1,1)
        /// </code><br/><br/>
        /// For alpha:<br/><br/>
        /// 1
        /// </summary>
        VK_BLEND_FACTOR_ONE = 1,
    }

    public enum VkBlendOp : int
    {
        /// <summary>
        /// Specifies a blend add operation.<br/><br/>
        /// For RGB:
        /// <code>
        /// R = Rs0 × Sr + Rd × Dr
        /// G = Gs0 × Sg + Gd × Dg
        /// B = Bs0 × Sb + Bd × Db
        /// </code><br/><br/>
        /// For alpha:<br/><br/>
        /// A = As0 × Sa + Ad × Da
        /// </summary>
        VK_BLEND_OP_ADD = 0,
    }

    public enum VkLogicOp : int
    {
        /// <summary>
        /// Specifies a copy operation.
        /// </summary>
        VK_LOGIC_OP_COPY = 3,
    }

    public enum VkCommandBufferLevel : int
    {
        /// <summary>
        /// Specifies a primary command buffer.
        /// </summary>
        VK_COMMAND_BUFFER_LEVEL_PRIMARY = 0,
    }

    public enum VkSubpassContents : int
    {
        /// <summary>
        /// Specifies that the contents of the subpass will be recorded inline in the primary command buffer, and secondary command buffers <b>must</b> not be executed within the subpass.
        /// </summary>
        VK_SUBPASS_CONTENTS_INLINE = 0,
    }

    public enum VkIndexType : int
    {
        /// <summary>
        /// Specifies that indices are 16-bit unsigned integer values.
        /// </summary>
        VK_INDEX_TYPE_UINT16 = 0,
        /// <summary>
        /// Specifies that indices are 32-bit unsigned integer values.
        /// </summary>
        VK_INDEX_TYPE_UINT32 = 1,
    }

    public enum VkVertexInputRate : int
    {
        /// <summary>
        /// Specifies that vertex attribute addressing is a function of the vertex index.
        /// </summary>
        VK_VERTEX_INPUT_RATE_VERTEX = 0,
    }

    public enum VkBufferUsageFlagBits : int
    {
        /// <summary>
        /// Specifies that the buffer is suitable for passing as an element of the <c>pBuffers</c> array to <see cref="VK.vkCmdBindVertexBuffers"/>.
        /// </summary>
        VK_BUFFER_USAGE_VERTEX_BUFFER_BIT = 0x00000080,
        /// <summary>
        /// Specifies that the buffer is suitable for passing as the <c>buffer</c> parameter to vkCmdBindIndexBuffer2 and vkCmdBindIndexBuffer.
        /// </summary>
        VK_BUFFER_USAGE_INDEX_BUFFER_BIT = 0x00000040,
    }

    public enum VkMemoryPropertyFlagBits : int
    {
        /// <summary>
        /// Specifies that memory allocated with this type can be mapped for host access using <see cref="VK.vkMapMemory"/>.
        /// </summary>
        VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT = 0x00000002,
        /// <summary>
        /// Specifies that the host cache management commands vkFlushMappedMemoryRanges and vkInvalidateMappedMemoryRanges are not needed to manage <see href="https://docs.vulkan.org/spec/latest/chapters/synchronization.html#synchronization-dependencies-available-and-visible">availability and visibility</see> on the host.
        /// </summary>
        VK_MEMORY_PROPERTY_HOST_COHERENT_BIT = 0x00000004,
    }

    public enum VkSharingMode : int
    {
        /// <summary>
        /// Specifies that access to any range or image subresource of the object will be exclusive to a single queue family at a time.
        /// </summary>
        VK_SHARING_MODE_EXCLUSIVE  = 0,
        /// <summary>
        /// Specifies that concurrent access to any range or image subresource of the object from multiple queue families is supported.
        /// </summary>
        VK_SHARING_MODE_CONCURRENT = 1,
    }

    public enum VkImageUsageFlagBits : int
    {
        /// <summary>
        /// Specifies that the image can be used to create a VkImageView suitable for use as a color or resolve attachment in a VkFramebuffer
        /// </summary>
        VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT = 0x00000010,
    }

    public enum VkCompositeAlphaFlagBitsKHR : int
    {
        /// <summary>
        /// The alpha component, if it exists, of the images is ignored in the compositing process. Instead, the image is treated as if it has a constant alpha of 1.0.
        /// </summary>
        VK_COMPOSITE_ALPHA_OPAQUE_BIT_KHR = 0x00000001,
    }

    public enum VkImageViewType : int
    {
        /// <summary>
        /// Specifies a 2D image view type.
        /// </summary>
        VK_IMAGE_VIEW_TYPE_2D = 1,
    }

    public enum VkImageAspectFlagBits : int
    {
        /// <summary>
        /// Specifies the color aspect.
        /// </summary>
        VK_IMAGE_ASPECT_COLOR_BIT = 0x00000001,
    }

    public enum VkPipelineStageFlagBits : int
    {
        /// <summary>
        /// Specifies the stage of the pipeline after blending where the final color values are output from the pipeline.
        /// </summary>
        VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT = 0x00000400,
    }

    public enum VkDynamicState : int
    {
        /// <summary>
        /// Specifies that pViewports state in <see cref="VkPipelineViewportStateCreateInfo"/> will be ignored and must be dynamically set with <see cref="VK.vkCmdSetViewport"/> before any drawing commands.
        /// </summary>
        VK_DYNAMIC_STATE_VIEWPORT = 0,
        /// <summary>
        /// Specifies that pScissors state in <see cref="VkPipelineViewportStateCreateInfo"/> will be ignored and must be dynamically set with <see cref="VK.vkCmdSetScissor"/> before any drawing commands.
        /// </summary>
        VK_DYNAMIC_STATE_SCISSOR  = 1,
    }

    public enum VkFenceCreateFlagBits : int
    {
        /// <summary>
        /// Specifies that the fence object is created in the signaled state.
        /// </summary>
        VK_FENCE_CREATE_SIGNALED_BIT = 0x00000001,
    }

    // Structs
    [StructLayout(LayoutKind.Sequential)]
    public struct VkExtent2D
    {
        public uint Width;
        public uint Height;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VkOffset2D
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VkRect2D
    {
        public VkOffset2D Offset;
        public VkExtent2D Extent;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VkViewport
    {
        public float X;
        public float Y;
        public float Width;
        public float Height;
        public float MinDepth;
        public float MaxDepth;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VkClearColorValue
    {
        public float R, G, B, A;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VkClearValue
    {
        public VkClearColorValue Color;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VkSurfaceFormatKHR
    {
        public VkFormat Format;
        public VkColorSpaceKHR ColorSpace;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VkSurfaceCapabilitiesKHR
    {
        public uint MinImageCount;
        public uint MaxImageCount;
        public VkExtent2D CurrentExtent;
        public VkExtent2D MinImageExtent;
        public VkExtent2D MaxImageExtent;
        public uint MaxImageArrayLayers;
        public uint SupportedTransforms;
        public uint CurrentTransform;
        public uint SupportedCompositeAlpha;
        public uint SupportedUsageFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VkQueueFamilyProperties
    {
        public uint QueueFlags;
        public uint QueueCount;
        public uint TimestampValidBits;
        public VkExtent3D MinImageTransferGranularity;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VkExtent3D
    {
        public uint Width;
        public uint Height;
        public uint Depth;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VkPhysicalDeviceMemoryProperties
    {
        /// <summary>
        /// The number of valid elements in the <c>memoryTypes</c> array.
        /// </summary>
        public uint MemoryTypeCount;

        /// <summary>
        /// An array of VK_MAX_MEMORY_TYPES <see cref="VkMemoryType"/> structures describing the memory types that can be used to access memory allocated from the heaps specified by <c>memoryHeaps</c>.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public VkMemoryType[] MemoryTypes;

        /// <summary>
        /// The number of valid elements in the <c>memoryHeaps</c> array.
        /// </summary>
        public uint MemoryHeapCount;

        /// <summary>
        /// An array of VK_MAX_MEMORY_HEAPS <see cref="VkMemoryHeap"/> structures describing the memory heaps from which memory can be allocated.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public VkMemoryHeap[] MemoryHeaps;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VkMemoryType
    {
        public uint PropertyFlags;
        public uint HeapIndex;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VkMemoryHeap
    {
        public ulong Size;
        public uint Flags;
    }

    // P/Invoke
    public static unsafe class VK
    {
        const string VulkanLib = "/usr/local/lib/libvulkan.1.dylib";

        /// <summary>
        /// Creates an instance object.
        /// </summary>
        /// <param name="pCreateInfo">Pointer to a VkInstanceCreateInfo structure controlling creation of the instance</param>
        /// <param name="pAllocator">Host memory controller for allocation as described in the <see href="https://docs.vulkan.org/spec/latest/chapters/memory.html#memory-allocation">Memory Allocation</see> chapter.</param>
        /// <param name="pInstance">Points a VkInstance handle in which the resulting instance is returned.</param>
        /// <returns>The result of the operation.</returns>
        [DllImport(VulkanLib)]
        public static extern VkResult vkCreateInstance(ref VkInstanceCreateInfo pCreateInfo, IntPtr pAllocator, out IntPtr pInstance);

        [DllImport(VulkanLib)]
        public static extern void vkDestroyInstance(IntPtr instance, IntPtr pAllocator);

        /// <summary>
        /// Retrieves a list of physical device objects representing the physical devices installed in the system.
        /// </summary>
        /// <param name="instance">Handle to a Vulkan instance previously created with vkCreateInstance.</param>
        /// <param name="pPhysicalDeviceCount">Pointer to an integer related to the number of physical devices available or queried, as described below.</param>
        /// <param name="pPhysicalDevices">Either <see langword="null"/> or a pointer to an array of VkPhysicalDevice handles.</param>
        /// <returns>The result of the operation.</returns>
        [DllImport(VulkanLib)]
        public static extern VkResult vkEnumeratePhysicalDevices(IntPtr instance, ref uint pPhysicalDeviceCount, IntPtr* pPhysicalDevices);

        /// <summary>
        /// Queries properties of queues available on a physical device.
        /// </summary>
        /// <param name="physicalDevice">Handle to the physical device whose properties will be queried.</param>
        /// <param name="pQueueFamilyPropertyCount">Pointer to an integer related to the number of queue families available or queried, as described below.</param>
        /// <param name="pQueueFamilyProperties">Either <see langword="null"/> or a pointer to an array of VkQueueFamilyProperties structures.</param>
        [DllImport(VulkanLib)]
        public static extern void vkGetPhysicalDeviceQueueFamilyProperties(IntPtr physicalDevice, ref uint pQueueFamilyPropertyCount, VkQueueFamilyProperties* pQueueFamilyProperties);

        /// <summary>
        /// Creates a logical device. A logical device is created as a <i>connection</i> to a physical device.
        /// </summary>
        /// <param name="physicalDevice">The physical device. Must be one of the device handles returned from a call to vkEnumeratePhysicalDevices.</param>
        /// <param name="pCreateInfo">A pointer to a VkDeviceCreateInfo structure containing information about how to create the device.</param>
        /// <param name="pAllocator">Host memory controller for allocation as described in the <see href="https://docs.vulkan.org/spec/latest/chapters/memory.html#memory-allocation">Memory Allocation</see> chapter.</param>
        /// <param name="pDevice">Pointer to a handle in which the created VkDevice is returned.</param>
        /// <returns>The result of the operation.</returns>
        [DllImport(VulkanLib)]
        public static extern VkResult vkCreateDevice(IntPtr physicalDevice, ref VkDeviceCreateInfo pCreateInfo, IntPtr pAllocator, out IntPtr pDevice);

        [DllImport(VulkanLib)]
        public static extern void vkGetDeviceQueue(IntPtr device, uint queueFamilyIndex, uint queueIndex, out IntPtr pQueue);

        [DllImport(VulkanLib)]
        public static extern VkResult vkCreateCommandPool(IntPtr device, ref VkCommandPoolCreateInfo pCreateInfo, IntPtr pAllocator, out IntPtr pCommandPool);

        [DllImport(VulkanLib)]
        public static extern VkResult vkAllocateCommandBuffers(IntPtr device, ref VkCommandBufferAllocateInfo pAllocateInfo, IntPtr* pCommandBuffers);

        [DllImport(VulkanLib)]
        public static extern VkResult vkBeginCommandBuffer(IntPtr commandBuffer, ref VkCommandBufferBeginInfo pBeginInfo);

        [DllImport(VulkanLib)]
        public static extern VkResult vkEndCommandBuffer(IntPtr commandBuffer);

        [DllImport(VulkanLib)]
        public static extern void vkCmdBeginRenderPass(IntPtr commandBuffer, ref VkRenderPassBeginInfo pRenderPassBegin, VkSubpassContents contents);

        [DllImport(VulkanLib)]
        public static extern void vkCmdEndRenderPass(IntPtr commandBuffer);

        [DllImport(VulkanLib)]
        public static extern void vkCmdBindPipeline(IntPtr commandBuffer, VkPipelineBindPoint pipelineBindPoint, IntPtr pipeline);

        [DllImport(VulkanLib)]
        public static extern void vkCmdDraw(IntPtr commandBuffer, uint vertexCount, uint instanceCount, uint firstVertex, uint firstInstance);

        [DllImport(VulkanLib)]
        public static extern void vkCmdSetViewport(IntPtr commandBuffer, uint firstViewport, uint viewportCount, ref VkViewport pViewports);

        [DllImport(VulkanLib)]
        public static extern void vkCmdSetScissor(IntPtr commandBuffer, uint firstScissor, uint scissorCount, ref VkRect2D pScissors);

        [DllImport(VulkanLib)]
        public static extern VkResult vkQueueSubmit(IntPtr queue, uint submitCount, ref VkSubmitInfo pSubmits, IntPtr fence);

        [DllImport(VulkanLib)]
        public static extern VkResult vkQueuePresentKHR(IntPtr queue, ref VkPresentInfoKHR pPresentInfo);

        [DllImport(VulkanLib)]
        public static extern VkResult vkAcquireNextImageKHR(IntPtr device, IntPtr swapchain, ulong timeout, IntPtr semaphore, IntPtr fence, out uint pImageIndex);

        [DllImport(VulkanLib)]
        public static extern VkResult vkCreateSwapchainKHR(IntPtr device, ref VkSwapchainCreateInfoKHR pCreateInfo, IntPtr pAllocator, out IntPtr pSwapchain);

        [DllImport(VulkanLib)]
        public static extern VkResult vkGetSwapchainImagesKHR(IntPtr device, IntPtr swapchain, ref uint pSwapchainImageCount, IntPtr* pSwapchainImages);

        [DllImport(VulkanLib)]
        public static extern VkResult vkCreateImageView(IntPtr device, ref VkImageViewCreateInfo pCreateInfo, IntPtr pAllocator, out IntPtr pView);

        [DllImport(VulkanLib)]
        public static extern VkResult vkCreateRenderPass(IntPtr device, ref VkRenderPassCreateInfo pCreateInfo, IntPtr pAllocator, out IntPtr pRenderPass);

        [DllImport(VulkanLib)]
        public static extern VkResult vkCreateFramebuffer(IntPtr device, ref VkFramebufferCreateInfo pCreateInfo, IntPtr pAllocator, out IntPtr pFramebuffer);

        [DllImport(VulkanLib)]
        public static extern VkResult vkCreateShaderModule(IntPtr device, ref VkShaderModuleCreateInfo pCreateInfo, IntPtr pAllocator, out IntPtr pShaderModule);

        [DllImport(VulkanLib)]
        public static extern VkResult vkCreatePipelineLayout(IntPtr device, ref VkPipelineLayoutCreateInfo pCreateInfo, IntPtr pAllocator, out IntPtr pPipelineLayout);

        [DllImport(VulkanLib)]
        public static extern VkResult vkCreateGraphicsPipelines(IntPtr device, IntPtr pipelineCache, uint createInfoCount, ref VkGraphicsPipelineCreateInfo pCreateInfos, IntPtr pAllocator, out IntPtr pPipelines);

        /// <summary>
        /// Creates a <see href="https://docs.vulkan.org/spec/latest/chapters/synchronization.html#synchronization-semaphores">semaphore</see>.
        /// </summary>
        /// <param name="device">Logical device that creates the semaphore.</param>
        /// <param name="pCreateInfo">Pointer to a VkSemaphoreCreateInfo structure containing information about how the semaphore is to be created.</param>
        /// <param name="pAllocator">Host memory controller for allocation as described in the <see href="https://docs.vulkan.org/spec/latest/chapters/memory.html#memory-allocation">Memory Allocation</see> chapter.</param>
        /// <param name="pSemaphore">Pointer to a handle in which the resulting semaphore object is returned.</param>
        /// <returns>The result of the operation.</returns>
        [DllImport(VulkanLib)]
        public static extern VkResult vkCreateSemaphore(IntPtr device, ref VkSemaphoreCreateInfo pCreateInfo, IntPtr pAllocator, out IntPtr pSemaphore);

        [DllImport(VulkanLib)]
        public static extern VkResult vkCreateFence(IntPtr device, ref VkFenceCreateInfo pCreateInfo, IntPtr pAllocator, out IntPtr pFence);

        [DllImport(VulkanLib)]
        public static extern VkResult vkWaitForFences(IntPtr device, uint fenceCount, ref IntPtr pFences, uint waitAll, ulong timeout);

        [DllImport(VulkanLib)]
        public static extern VkResult vkResetFences(IntPtr device, uint fenceCount, ref IntPtr pFences);

        [DllImport(VulkanLib)]
        public static extern VkResult vkResetCommandBuffer(IntPtr commandBuffer, uint flags);

        [DllImport(VulkanLib)]
        public static extern VkResult vkGetPhysicalDeviceSurfaceCapabilitiesKHR(IntPtr physicalDevice, IntPtr surface, out VkSurfaceCapabilitiesKHR pSurfaceCapabilities);

        [DllImport(VulkanLib)]
        public static extern VkResult vkGetPhysicalDeviceSurfaceFormatsKHR(IntPtr physicalDevice, IntPtr surface, ref uint pSurfaceFormatCount, VkSurfaceFormatKHR* pSurfaceFormats);

        [DllImport(VulkanLib)]
        public static extern VkResult vkGetPhysicalDeviceSurfacePresentModesKHR(IntPtr physicalDevice, IntPtr surface, ref uint pPresentModeCount, VkPresentModeKHR* pPresentModes);

        /// <summary>
        /// Determine whether a queue family of a physical device supports presentation to a given surface.
        /// </summary>
        /// <param name="physicalDevice">The physical device.</param>
        /// <param name="queueFamilyIndex">The queue family.</param>
        /// <param name="surface">The surface.</param>
        /// <param name="pSupported">Pointer to a VkBool32. VK_TRUE indicates support, and VK_FALSE indicates no support.</param>
        /// <returns>The result of the operation.</returns>
        [DllImport(VulkanLib)]
        public static extern VkResult vkGetPhysicalDeviceSurfaceSupportKHR(IntPtr physicalDevice, uint queueFamilyIndex, IntPtr surface, out uint pSupported);

        [DllImport(VulkanLib)]
        public static extern void vkGetPhysicalDeviceMemoryProperties(IntPtr physicalDevice, out VkPhysicalDeviceMemoryProperties pMemoryProperties);

        [DllImport(VulkanLib)]
        public static extern VkResult vkCreateBuffer(IntPtr device, ref VkBufferCreateInfo pCreateInfo, IntPtr pAllocator, out IntPtr pBuffer);

        [DllImport(VulkanLib)]
        public static extern void vkGetBufferMemoryRequirements(IntPtr device, IntPtr buffer, out VkMemoryRequirements pMemoryRequirements);

        [DllImport(VulkanLib)]
        public static extern VkResult vkAllocateMemory(IntPtr device, ref VkMemoryAllocateInfo pAllocateInfo, IntPtr pAllocator, out IntPtr pMemory);

        [DllImport(VulkanLib)]
        public static extern VkResult vkBindBufferMemory(IntPtr device, IntPtr buffer, IntPtr memory, ulong memoryOffset);

        /// <summary>
        /// Retrieves a host virtual address pointer to a region of a mappable memory object.
        /// </summary>
        /// <param name="device">Logical device that owns the memory.</param>
        /// <param name="memory">The VkDeviceMemory object to be mapped.</param>
        /// <param name="offset">Zero-based byte offset from the beginning of the memory object.</param>
        /// <param name="size">The size of the memory range to map, or VK_WHOLE_SIZE to map from offset to the end of the allocation.</param>
        /// <param name="flags">A bitmask of VkMemoryMapFlagBits specifying additional parameters of the memory map operation.</param>
        /// <param name="ppData">A pointer to a <c>void*</c> variable in which a host-accessible pointer to the beginning of the mapped range is returned. The value of the returned pointer minus <c>offset</c> must be aligned to VkPhysicalDeviceLimits::<c>minMemoryMapAlignment</c>.</param>
        /// <returns>The result of the operation.</returns>
        [DllImport(VulkanLib)]
        public static extern VkResult vkMapMemory(IntPtr device, IntPtr memory, ulong offset, ulong size, uint flags, out IntPtr ppData);

        [DllImport(VulkanLib)]
        public static extern void vkUnmapMemory(IntPtr device, IntPtr memory);

        [DllImport(VulkanLib)]
        public static extern void vkCmdBindVertexBuffers(IntPtr commandBuffer, uint firstBinding, uint bindingCount, ref IntPtr pBuffers, ref ulong pOffsets);

        [DllImport(VulkanLib)]
        public static extern VkResult vkDeviceWaitIdle(IntPtr device);

        [DllImport(VulkanLib)]
        public static extern void vkDestroyDevice(IntPtr device, IntPtr pAllocator);

        [DllImport(VulkanLib)]
        public static extern void vkDestroySwapchainKHR(IntPtr device, IntPtr swapchain, IntPtr pAllocator);

        [DllImport(VulkanLib)]
        public static extern void vkDestroySurfaceKHR(IntPtr instance, IntPtr surface, IntPtr pAllocator);

        // MoltenVK surface
        [DllImport(VulkanLib)]
        public static extern VkResult vkCreateMacOSSurfaceMVK(IntPtr instance, ref VkMacOSSurfaceCreateInfoMVK pCreateInfo, IntPtr pAllocator, out IntPtr pSurface);

        // Helper
        public static void Check(VkResult result, string message)
        {
            if (result != VkResult.VK_SUCCESS)
                throw new Exception($"Vulkan error in {message}: {result}");
        }
    }

    /// <summary>
    /// Structure specifying application information.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct VkApplicationInfo
    {
        public VkStructureType SType;
        public IntPtr PNext;
        public IntPtr PApplicationName;
        public uint ApplicationVersion;
        public IntPtr PEngineName;
        public uint EngineVersion;
        public uint ApiVersion;
    }

    /// <summary>
    /// Structure specifying parameters of a newly created instance.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct VkInstanceCreateInfo
    {
        public VkStructureType SType;
        public IntPtr PNext;
        public uint Flags;
        public IntPtr PApplicationInfo;
        public uint EnabledLayerCount;
        public IntPtr PpEnabledLayerNames;
        public uint EnabledExtensionCount;
        public IntPtr PpEnabledExtensionNames;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct VkDeviceQueueCreateInfo
    {
        public VkStructureType SType;
        public IntPtr PNext;
        public uint Flags;
        public uint QueueFamilyIndex;
        public uint QueueCount;
        public float* PQueuePriorities;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct VkDeviceCreateInfo
    {
        public VkStructureType SType;
        public IntPtr PNext;
        public uint Flags;
        public uint QueueCreateInfoCount;
        public VkDeviceQueueCreateInfo* PQueueCreateInfos;
        public uint EnabledLayerCount;
        public IntPtr PpEnabledLayerNames;
        public uint EnabledExtensionCount;
        public IntPtr PpEnabledExtensionNames;
        public IntPtr PEnabledFeatures;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VkCommandPoolCreateInfo
    {
        public VkStructureType SType;
        public IntPtr PNext;
        public uint Flags;
        public uint QueueFamilyIndex;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VkCommandBufferAllocateInfo
    {
        public VkStructureType SType;
        public IntPtr PNext;
        public IntPtr CommandPool;
        public VkCommandBufferLevel Level;
        public uint CommandBufferCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VkCommandBufferBeginInfo
    {
        public VkStructureType SType;
        public IntPtr PNext;
        public uint Flags;
        public IntPtr PInheritanceInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct VkRenderPassBeginInfo
    {
        public VkStructureType SType;
        public IntPtr PNext;
        public IntPtr RenderPass;
        public IntPtr Framebuffer;
        public VkRect2D RenderArea;
        public uint ClearValueCount;
        public VkClearValue* PClearValues;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct VkSubmitInfo
    {
        public VkStructureType SType;
        public IntPtr PNext;
        public uint WaitSemaphoreCount;
        public IntPtr* PWaitSemaphores;
        public int* PWaitDstStageMask;
        public uint CommandBufferCount;
        public IntPtr* PCommandBuffers;
        public uint SignalSemaphoreCount;
        public IntPtr* PSignalSemaphores;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct VkPresentInfoKHR
    {
        public VkStructureType SType;
        public IntPtr PNext;
        public uint WaitSemaphoreCount;
        public IntPtr* PWaitSemaphores;
        public uint SwapchainCount;
        public IntPtr* PSwapchains;
        public uint* PImageIndices;
        public VkResult* PResults;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct VkSwapchainCreateInfoKHR
    {
        public VkStructureType SType;
        public IntPtr PNext;
        public uint Flags;
        public IntPtr Surface;
        public uint MinImageCount;
        public VkFormat ImageFormat;
        public VkColorSpaceKHR ImageColorSpace;
        public VkExtent2D ImageExtent;
        public uint ImageArrayLayers;
        public uint ImageUsage;
        public VkSharingMode ImageSharingMode;
        public uint QueueFamilyIndexCount;
        public uint* PQueueFamilyIndices;
        public uint PreTransform;
        public VkCompositeAlphaFlagBitsKHR CompositeAlpha;
        public VkPresentModeKHR PresentMode;
        public uint Clipped;
        public IntPtr OldSwapchain;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VkImageViewCreateInfo
    {
        public VkStructureType SType;
        public IntPtr PNext;
        public uint Flags;
        public IntPtr Image;
        public VkImageViewType ViewType;
        public VkFormat Format;
        public VkComponentMapping Components;
        public VkImageSubresourceRange SubresourceRange;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VkComponentMapping
    {
        public uint R, G, B, A;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VkImageSubresourceRange
    {
        public uint AspectMask;
        public uint BaseMipLevel;
        public uint LevelCount;
        public uint BaseArrayLayer;
        public uint LayerCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VkAttachmentDescription
    {
        public uint Flags;
        public VkFormat Format;
        public VkSampleCountFlagBits Samples;
        public VkAttachmentLoadOp LoadOp;
        public VkAttachmentStoreOp StoreOp;
        public VkAttachmentLoadOp StencilLoadOp;
        public VkAttachmentStoreOp StencilStoreOp;
        public VkImageLayout InitialLayout;
        public VkImageLayout FinalLayout;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VkAttachmentReference
    {
        public uint Attachment;
        public VkImageLayout Layout;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct VkSubpassDescription
    {
        public uint Flags;
        public VkPipelineBindPoint PipelineBindPoint;
        public uint InputAttachmentCount;
        public VkAttachmentReference* PInputAttachments;
        public uint ColorAttachmentCount;
        public VkAttachmentReference* PColorAttachments;
        public IntPtr PResolveAttachments;
        public IntPtr PDepthStencilAttachment;
        public uint PreserveAttachmentCount;
        public uint* PPreserveAttachments;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct VkRenderPassCreateInfo
    {
        public VkStructureType SType;
        public IntPtr PNext;
        public uint Flags;
        public uint AttachmentCount;
        public VkAttachmentDescription* PAttachments;
        public uint SubpassCount;
        public VkSubpassDescription* PSubpasses;
        public uint DependencyCount;
        public IntPtr PDependencies;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct VkFramebufferCreateInfo
    {
        public VkStructureType SType;
        public IntPtr PNext;
        public uint Flags;
        public IntPtr RenderPass;
        public uint AttachmentCount;
        public IntPtr* PAttachments;
        public uint Width;
        public uint Height;
        public uint Layers;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct VkShaderModuleCreateInfo
    {
        public VkStructureType SType;
        public IntPtr PNext;
        public uint Flags;
        public UIntPtr CodeSize;
        public uint* PCode;
    }

    /// <summary>
    /// Structure specifying parameters of a newly created pipeline shader stage.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct VkPipelineShaderStageCreateInfo
    {
        public VkStructureType SType;
        public IntPtr PNext;
        public uint Flags;
        public VkShaderStageFlagBits Stage;
        public IntPtr Module;
        public IntPtr PName;
        public IntPtr PSpecializationInfo;
    }

    /// <summary>
    /// Structure specifying vertex input binding description.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct VkVertexInputBindingDescription
    {
        public uint Binding;
        public uint Stride;
        public VkVertexInputRate InputRate;
    }

    /// <summary>
    /// Structure specifying vertex input attribute description.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct VkVertexInputAttributeDescription
    {
        public uint Location;
        public uint Binding;
        public VkFormat Format;
        public uint Offset;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct VkPipelineVertexInputStateCreateInfo
    {
        public VkStructureType SType;
        public IntPtr PNext;
        public uint Flags;
        public uint VertexBindingDescriptionCount;
        public VkVertexInputBindingDescription* PVertexBindingDescriptions;
        public uint VertexAttributeDescriptionCount;
        public VkVertexInputAttributeDescription* PVertexAttributeDescriptions;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VkPipelineInputAssemblyStateCreateInfo
    {
        public VkStructureType SType;
        public IntPtr PNext;
        public uint Flags;
        public VkPrimitiveTopology Topology;
        public uint PrimitiveRestartEnable;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct VkPipelineViewportStateCreateInfo
    {
        public VkStructureType SType;
        public IntPtr PNext;
        public uint Flags;
        public uint ViewportCount;
        public VkViewport* PViewports;
        public uint ScissorCount;
        public VkRect2D* PScissors;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VkPipelineRasterizationStateCreateInfo
    {
        public VkStructureType SType;
        public IntPtr PNext;
        public uint Flags;
        public uint DepthClampEnable;
        public uint RasterizerDiscardEnable;
        public VkPolygonMode PolygonMode;
        public uint CullMode;
        public VkFrontFace FrontFace;
        public uint DepthBiasEnable;
        public float DepthBiasConstantFactor;
        public float DepthBiasClamp;
        public float DepthBiasSlopeFactor;
        public float LineWidth;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VkPipelineMultisampleStateCreateInfo
    {
        public VkStructureType SType;
        public IntPtr PNext;
        public uint Flags;
        public VkSampleCountFlagBits RasterizationSamples;
        public uint SampleShadingEnable;
        public float MinSampleShading;
        public IntPtr PSampleMask;
        public uint AlphaToCoverageEnable;
        public uint AlphaToOneEnable;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VkPipelineColorBlendAttachmentState
    {
        public uint BlendEnable;
        public VkBlendFactor SrcColorBlendFactor;
        public VkBlendFactor DstColorBlendFactor;
        public VkBlendOp ColorBlendOp;
        public VkBlendFactor SrcAlphaBlendFactor;
        public VkBlendFactor DstAlphaBlendFactor;
        public VkBlendOp AlphaBlendOp;
        public uint ColorWriteMask;
    }

    /// <summary>
    /// Structure specifying parameters of a newly created pipeline color blend state.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct VkPipelineColorBlendStateCreateInfo
    {
        public VkStructureType SType;
        public IntPtr PNext;
        public uint Flags;
        /// <summary>
        /// Controls whether to apply Logical Operations.
        /// </summary>
        public uint LogicOpEnable;
        /// <summary>
        /// Selects which logical operation to apply.
        /// </summary>
        public VkLogicOp LogicOp;
        /// <summary>
        /// The number of VkPipelineColorBlendAttachmentState elements in <c>pAttachments</c>.
        /// </summary>
        public uint AttachmentCount;
        public VkPipelineColorBlendAttachmentState* PAttachments;
        public fixed float BlendConstants[4];
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VkPipelineLayoutCreateInfo
    {
        public VkStructureType SType;
        public IntPtr PNext;
        public uint Flags;
        public uint SetLayoutCount;
        public IntPtr PSetLayouts;
        public uint PushConstantRangeCount;
        public IntPtr PPushConstantRanges;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct VkPipelineDynamicStateCreateInfo
    {
        public VkStructureType SType;
        public IntPtr PNext;
        public uint Flags;
        public uint DynamicStateCount;
        public VkDynamicState* PDynamicStates;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct VkGraphicsPipelineCreateInfo
    {
        public VkStructureType SType;
        public IntPtr PNext;
        public uint Flags;
        public uint StageCount;
        public VkPipelineShaderStageCreateInfo* PStages;
        public VkPipelineVertexInputStateCreateInfo* PVertexInputState;
        public VkPipelineInputAssemblyStateCreateInfo* PInputAssemblyState;
        public IntPtr PTessellationState;
        public VkPipelineViewportStateCreateInfo* PViewportState;
        public VkPipelineRasterizationStateCreateInfo* PRasterizationState;
        public VkPipelineMultisampleStateCreateInfo* PMultisampleState;
        public IntPtr PDepthStencilState;
        public VkPipelineColorBlendStateCreateInfo* PColorBlendState;
        public VkPipelineDynamicStateCreateInfo* PDynamicState;
        public IntPtr Layout;
        public IntPtr RenderPass;
        public uint Subpass;
        public IntPtr BasePipelineHandle;
        public int BasePipelineIndex;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VkSemaphoreCreateInfo
    {
        public VkStructureType SType;
        public IntPtr PNext;
        public uint Flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VkFenceCreateInfo
    {
        public VkStructureType SType;
        public IntPtr PNext;
        public VkFenceCreateFlagBits Flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VkMacOSSurfaceCreateInfoMVK
    {
        public VkStructureType SType;
        public IntPtr PNext;
        public uint Flags;
        public IntPtr PView;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct VkBufferCreateInfo
    {
        public VkStructureType SType;
        public IntPtr PNext;
        public uint Flags;
        public ulong Size;
        public uint Usage;
        public VkSharingMode SharingMode;
        public uint QueueFamilyIndexCount;
        public uint* PQueueFamilyIndices;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VkMemoryRequirements
    {
        public ulong Size;
        public ulong Alignment;
        public uint MemoryTypeBits;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VkMemoryAllocateInfo
    {
        public VkStructureType SType;
        public IntPtr PNext;
        public ulong AllocationSize;
        public uint MemoryTypeIndex;
    }
}