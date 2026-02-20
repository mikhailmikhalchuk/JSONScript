using System;
using System.Runtime.InteropServices;

namespace JSONScript.VM.Graphics.Vulkan
{
    /// <summary>
    /// Represents a Vulkan swapchain, which is a series of images used for rendering and presenting frames to a window or surface.
    /// </summary>
    public unsafe class VulkanSwapchain
    {
        public IntPtr Swapchain { get; private set; }
        public IntPtr[] Images { get; private set; } = Array.Empty<IntPtr>();
        public IntPtr[] ImageViews { get; private set; } = Array.Empty<IntPtr>();
        public VkFormat Format { get; private set; }
        public VkExtent2D Extent { get; private set; }

        private readonly VulkanDevice device;
        private readonly IntPtr surface;

        public VulkanSwapchain(VulkanDevice device, IntPtr surface)
        {
            this.device = device;
            this.surface = surface;
        }

        public void Init(int width, int height)
        {
            CreateSwapchain(width, height);
            RetrieveImages();
            CreateImageViews();
            Console.WriteLine($"[Vulkan] Swapchain created ({Images.Length} images)");
        }

        private void CreateSwapchain(int width, int height)
        {
            //query surface capabilities
            VK.Check(VK.vkGetPhysicalDeviceSurfaceCapabilitiesKHR( device.PhysicalDevice, surface, out var caps), "vkGetPhysicalDeviceSurfaceCapabilitiesKHR");

            //query surface formats
            uint formatCount = 0;
            VK.vkGetPhysicalDeviceSurfaceFormatsKHR(device.PhysicalDevice, surface, ref formatCount, null);
            var formats = new VkSurfaceFormatKHR[formatCount];
            fixed (VkSurfaceFormatKHR* pFormats = formats)
                VK.vkGetPhysicalDeviceSurfaceFormatsKHR(device.PhysicalDevice, surface, ref formatCount, pFormats);

            //query present modes
            uint modeCount = 0;
            VK.vkGetPhysicalDeviceSurfacePresentModesKHR(device.PhysicalDevice, surface, ref modeCount, null);
            var presentModes = new VkPresentModeKHR[modeCount];
            fixed (VkPresentModeKHR* pModes = presentModes)
                VK.vkGetPhysicalDeviceSurfacePresentModesKHR(device.PhysicalDevice, surface, ref modeCount, pModes);

            //pick format - prefer B8G8R8A8_SRGB with SRGB_NONLINEAR
            var chosenFormat = formats[0];
            foreach (var f in formats)
            {
                if (f.Format == VkFormat.VK_FORMAT_B8G8R8A8_SRGB && f.ColorSpace == VkColorSpaceKHR.VK_COLOR_SPACE_SRGB_NONLINEAR_KHR)
                {
                    chosenFormat = f;
                    break;
                }
            }
            Format = chosenFormat.Format;

            //pick present mode - prefer mailbox b/c of no fps cap
            var chosenMode = VkPresentModeKHR.VK_PRESENT_MODE_FIFO_KHR;
            foreach (var m in presentModes)
            {
                if (m == VkPresentModeKHR.VK_PRESENT_MODE_MAILBOX_KHR)
                {
                    chosenMode = m;
                    break;
                }
            }

            //pick extent
            if (caps.CurrentExtent.Width != uint.MaxValue)
            {
                Extent = caps.CurrentExtent;
            }
            else
            {
                Extent = new VkExtent2D
                {
                    Width  = (uint)Math.Clamp(width, (int)caps.MinImageExtent.Width, (int)caps.MaxImageExtent.Width),
                    Height = (uint)Math.Clamp(height, (int)caps.MinImageExtent.Height, (int)caps.MaxImageExtent.Height)
                };
            }

            //image count — one more than minimum, capped at maximum
            uint imageCount = caps.MinImageCount + 1;
            if (caps.MaxImageCount > 0 && imageCount > caps.MaxImageCount)
                imageCount = caps.MaxImageCount;

            bool sameFamily = device.GraphicsFamily == device.PresentFamily;
            uint[] families = sameFamily ? new uint[] { device.GraphicsFamily } : new uint[] { device.GraphicsFamily, device.PresentFamily };

            fixed (uint* pFamilies = families)
            {
                var createInfo = new VkSwapchainCreateInfoKHR
                {
                    SType = VkStructureType.VK_STRUCTURE_TYPE_SWAPCHAIN_CREATE_INFO_KHR,
                    PNext = IntPtr.Zero,
                    Flags = 0,
                    Surface = surface,
                    MinImageCount = imageCount,
                    ImageFormat = chosenFormat.Format,
                    ImageColorSpace = chosenFormat.ColorSpace,
                    ImageExtent = Extent,
                    ImageArrayLayers = 1,
                    ImageUsage = (uint)VkImageUsageFlagBits.VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT,
                    ImageSharingMode = sameFamily ? VkSharingMode.VK_SHARING_MODE_EXCLUSIVE : VkSharingMode.VK_SHARING_MODE_CONCURRENT,
                    QueueFamilyIndexCount = (uint)families.Length,
                    PQueueFamilyIndices = pFamilies,
                    PreTransform = caps.CurrentTransform,
                    CompositeAlpha = VkCompositeAlphaFlagBitsKHR.VK_COMPOSITE_ALPHA_OPAQUE_BIT_KHR,
                    PresentMode = chosenMode,
                    Clipped = 1,
                    OldSwapchain = IntPtr.Zero
                };

                VK.Check(VK.vkCreateSwapchainKHR(device.Device, ref createInfo, IntPtr.Zero, out var sc), "vkCreateSwapchainKHR");
                Swapchain = sc;
            }
        }

        private void RetrieveImages()
        {
            uint count = 0;
            VK.vkGetSwapchainImagesKHR(device.Device, Swapchain, ref count, null);
            Images = new IntPtr[count];
            fixed (IntPtr* pImages = Images)
                VK.vkGetSwapchainImagesKHR(device.Device, Swapchain, ref count, pImages);
        }

        private void CreateImageViews()
        {
            ImageViews = new IntPtr[Images.Length];
            for (int i = 0; i < Images.Length; i++)
            {
                var createInfo = new VkImageViewCreateInfo
                {
                    SType = VkStructureType.VK_STRUCTURE_TYPE_IMAGE_VIEW_CREATE_INFO,
                    PNext = IntPtr.Zero,
                    Flags = 0,
                    Image = Images[i],
                    ViewType = VkImageViewType.VK_IMAGE_VIEW_TYPE_2D,
                    Format = Format,
                    Components = new VkComponentMapping { R = 0, G = 0, B = 0, A = 0 }, //identity
                    SubresourceRange = new VkImageSubresourceRange
                    {
                        AspectMask = (uint)VkImageAspectFlagBits.VK_IMAGE_ASPECT_COLOR_BIT,
                        BaseMipLevel = 0,
                        LevelCount = 1,
                        BaseArrayLayer = 0,
                        LayerCount = 1
                    }
                };

                VK.Check(VK.vkCreateImageView(device.Device, ref createInfo, IntPtr.Zero, out ImageViews[i]), $"vkCreateImageView[{i}]");
            }
        }

        public void Destroy()
        {
            foreach (var iv in ImageViews)
            {
                VK.vkDestroySwapchainKHR(device.Device, iv, IntPtr.Zero);
            }
            VK.vkDestroySwapchainKHR(device.Device, Swapchain, IntPtr.Zero);
        }
    }
}