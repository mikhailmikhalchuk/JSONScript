using System;
using System.Runtime.InteropServices;

namespace JSONScript.VM.Graphics.Vulkan
{
    public unsafe class VulkanDevice
    {
        public IntPtr Instance { get; private set; }
        public IntPtr PhysicalDevice { get; private set; }
        public IntPtr Device { get; private set; }
        public IntPtr GraphicsQueue { get; private set; }
        public IntPtr PresentQueue { get; private set; }
        public uint GraphicsFamily { get; private set; }
        public uint PresentFamily { get; private set; }

        private static readonly string[] ValidationLayers = { "VK_LAYER_KHRONOS_validation" };
        private static readonly string[] DeviceExtensions = { "VK_KHR_swapchain", "VK_KHR_portability_subset" };
        private static readonly string[] InstanceExtensions =
        {
            "VK_KHR_surface",
            "VK_MVK_macos_surface",
            "VK_KHR_portability_enumeration",
            "VK_KHR_get_physical_device_properties2"
        };

        public void Init()
        {
            CreateInstance();
            PickPhysicalDevice();
        }

        public void InitSurface(IntPtr surface)
        {
            FindQueueFamilies(surface);
            CreateLogicalDevice();
        }

        private void CreateInstance()
        {
            // Application info
            var appName = Marshal.StringToHGlobalAnsi("JSONScript");
            var engineName = Marshal.StringToHGlobalAnsi("JSONScriptVM");

            var appInfo = new VkApplicationInfo
            {
                SType = VkStructureType.VK_STRUCTURE_TYPE_APPLICATION_INFO,
                PNext = IntPtr.Zero,
                PApplicationName = appName,
                ApplicationVersion = MakeVersion(1, 0, 0),
                PEngineName = engineName,
                EngineVersion = MakeVersion(1, 0, 0),
                ApiVersion = MakeVersion(1, 2, 0)
            };

            // Pin appInfo
            var appInfoHandle = GCHandle.Alloc(appInfo, GCHandleType.Pinned);

            // Validation layers
            var layerPtrs = new IntPtr[ValidationLayers.Length];
            for (int i = 0; i < ValidationLayers.Length; i++)
                layerPtrs[i] = Marshal.StringToHGlobalAnsi(ValidationLayers[i]);

            // Extensions
            var extPtrs = new IntPtr[InstanceExtensions.Length];
            for (int i = 0; i < InstanceExtensions.Length; i++)
                extPtrs[i] = Marshal.StringToHGlobalAnsi(InstanceExtensions[i]);

            fixed (IntPtr* layerPtr = layerPtrs)
            fixed (IntPtr* extPtr   = extPtrs)
            {
                var createInfo = new VkInstanceCreateInfo
                {
                    SType = VkStructureType.VK_STRUCTURE_TYPE_INSTANCE_CREATE_INFO,
                    PNext = IntPtr.Zero,
                    Flags = 0x00000001, // VK_INSTANCE_CREATE_ENUMERATE_PORTABILITY_BIT_KHR
                    PApplicationInfo = appInfoHandle.AddrOfPinnedObject(),
                    EnabledLayerCount = (uint)ValidationLayers.Length,
                    PpEnabledLayerNames = (IntPtr)layerPtr,
                    EnabledExtensionCount = (uint)InstanceExtensions.Length,
                    PpEnabledExtensionNames = (IntPtr)extPtr
                };

                VK.Check(VK.vkCreateInstance(ref createInfo, IntPtr.Zero, out var instance), "vkCreateInstance");
                Instance = instance;
            }

            appInfoHandle.Free();
            Marshal.FreeHGlobal(appName);
            Marshal.FreeHGlobal(engineName);
            foreach (var p in layerPtrs)
            {
                Marshal.FreeHGlobal(p);
            }
            foreach (var p in extPtrs)
            {
                Marshal.FreeHGlobal(p);
            }
        }

        private void PickPhysicalDevice()
        {
            uint count = 0;
            VK.vkEnumeratePhysicalDevices(Instance, ref count, null);
            if (count == 0)
                throw new Exception("No Vulkan-capable GPU found");

            var devices = new IntPtr[count];
            fixed (IntPtr* ptr = devices)
                VK.vkEnumeratePhysicalDevices(Instance, ref count, ptr);

            // Just pick the first one for now
            PhysicalDevice = devices[0];
            Console.WriteLine($"[Vulkan] Picked physical device: {PhysicalDevice}");
        }

        private void FindQueueFamilies(IntPtr surface)
        {
            uint count = 0;
            VK.vkGetPhysicalDeviceQueueFamilyProperties(PhysicalDevice, ref count, null);

            var families = new VkQueueFamilyProperties[count];
            fixed (VkQueueFamilyProperties* ptr = families)
                VK.vkGetPhysicalDeviceQueueFamilyProperties(PhysicalDevice, ref count, ptr);

            bool foundGraphics = false;
            bool foundPresent = false;

            for (uint i = 0; i < count; i++)
            {
                // VK_QUEUE_GRAPHICS_BIT = 1
                if ((families[i].QueueFlags & 1) != 0 && !foundGraphics)
                {
                    GraphicsFamily = i;
                    foundGraphics = true;
                }

                VK.vkGetPhysicalDeviceSurfaceSupportKHR(PhysicalDevice, i, surface, out uint supported);
                if (supported == 1 && !foundPresent)
                {
                    PresentFamily = i;
                    foundPresent = true;
                }

                if (foundGraphics && foundPresent)
                    break;
            }

            if (!foundGraphics || !foundPresent)
                throw new Exception("Could not find required queue families");
        }

        private void CreateLogicalDevice()
        {
            float priority = 1.0f;
            float* pPriority = &priority;

            var uniqueFamilies = GraphicsFamily == PresentFamily ? new uint[] { GraphicsFamily } : new uint[] { GraphicsFamily, PresentFamily };

            var queueInfos = new VkDeviceQueueCreateInfo[uniqueFamilies.Length];

            for (int i = 0; i < uniqueFamilies.Length; i++)
            {
                queueInfos[i] = new VkDeviceQueueCreateInfo
                {
                    SType = VkStructureType.VK_STRUCTURE_TYPE_DEVICE_QUEUE_CREATE_INFO,
                    PNext = IntPtr.Zero,
                    Flags = 0,
                    QueueFamilyIndex = uniqueFamilies[i],
                    QueueCount = 1,
                    PQueuePriorities = pPriority
                };
            }

            var extPtrs = new IntPtr[DeviceExtensions.Length];
            for (int i = 0; i < DeviceExtensions.Length; i++)
                extPtrs[i] = Marshal.StringToHGlobalAnsi(DeviceExtensions[i]);

            fixed (VkDeviceQueueCreateInfo* pQueueInfos = queueInfos)
            fixed (IntPtr* pExtPtrs = extPtrs)
            {
                var createInfo = new VkDeviceCreateInfo
                {
                    SType = VkStructureType.VK_STRUCTURE_TYPE_DEVICE_CREATE_INFO,
                    PNext = IntPtr.Zero,
                    Flags = 0,
                    QueueCreateInfoCount = (uint)queueInfos.Length,
                    PQueueCreateInfos = pQueueInfos,
                    EnabledLayerCount = 0,
                    PpEnabledLayerNames = IntPtr.Zero,
                    EnabledExtensionCount = (uint)DeviceExtensions.Length,
                    PpEnabledExtensionNames = (IntPtr)pExtPtrs,
                    PEnabledFeatures = IntPtr.Zero
                };

                VK.Check(VK.vkCreateDevice(PhysicalDevice, ref createInfo, IntPtr.Zero, out var device), "vkCreateDevice");
                Device = device;
            }

            foreach (var p in extPtrs)
            {
                Marshal.FreeHGlobal(p);
            }

            VK.vkGetDeviceQueue(Device, GraphicsFamily, 0, out var gq);
            VK.vkGetDeviceQueue(Device, PresentFamily,  0, out var pq);
            GraphicsQueue = gq;
            PresentQueue  = pq;

            Console.WriteLine("[Vulkan] Logical device and queues created");
        }

        private static uint MakeVersion(uint major, uint minor, uint patch) => (major << 22) | (minor << 12) | patch;

        public void Destroy()
        {
            VK.vkDestroyDevice(Device, IntPtr.Zero);
            VK.vkDestroyInstance(Instance, IntPtr.Zero);
        }
    }
}