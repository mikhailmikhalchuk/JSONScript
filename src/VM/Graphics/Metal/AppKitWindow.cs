using System;
using System.Runtime.InteropServices;
using JSONScript.Runtime;

namespace JSONScript.VM.Graphics.Metal
{
    public class AppKitWindow
    {
        /// <summary>
        /// Startup function to call when running Cocoa code from a Carbon application.
        /// </summary>
        [DllImport("/System/Library/Frameworks/AppKit.framework/AppKit")]
        private static extern void NSApplicationLoad();

        /// <summary>
        /// Returns the name of a class as a string.
        /// </summary>
        /// <param name="aClass">A class.</param>
        /// <returns>A string containing the name of aClass. If aClass is nil, returns nil.</returns>
        [DllImport("/System/Library/Frameworks/Foundation.framework/Foundation")]
        private static extern IntPtr NSStringFromClass(IntPtr aClass);

        public nint Layer => Renderer?.LayerPtr ?? IntPtr.Zero;
        public nint Device => Renderer?.DevicePtr ?? IntPtr.Zero;

        public IntPtr Window { get; private set; }
        public IntPtr View { get; private set; }
        public IntPtr NSApp { get; private set; }

        public MetalRenderer? Renderer { get; private set; }
        
        public Action<string, Value[]>? OnEvent;

        private long GetKeyCode(IntPtr evt) => ObjC.MsgSend(evt, ObjC.RegisterName("keyCode"));

        public MetalRenderer InitMetal(int width, int height)
        {
            Renderer = new MetalRenderer(View, width, height);
            return Renderer;
        }
        public AppKitWindow(int width, int height, string title)
        {
            NSApplicationLoad();

            // Get NSApplication shared instance
            var nsAppClass = ObjC.GetClass("NSApplication");
            var sharedApp = ObjC.RegisterName("sharedApplication");
            NSApp = ObjC.MsgSend(nsAppClass, sharedApp);

            // Set activation policy to regular (shows in dock)
            var setPolicy = ObjC.RegisterName("setActivationPolicy:");
            ObjC.MsgSendVoid(NSApp, setPolicy, 0); // NSApplicationActivationPolicyRegular

            // Create NSWindow
            var nsWindowClass = ObjC.GetClass("NSWindow");
            var alloc = ObjC.RegisterName("alloc");
            var initWithRect = ObjC.RegisterName("initWithContentRect:styleMask:backing:defer:");

            var windowAlloc = ObjC.MsgSend(nsWindowClass, alloc);
            var frame = new CGRect(0, 0, width, height);

            // styleMask: titled | closable | resizable = 1 | 2 | 8 = 15
            // backing: buffered = 2
            Window = ObjC.MsgSend(windowAlloc, initWithRect, frame, 15, 2, 0);

            // Set title
            var nsStringClass = ObjC.GetClass("NSString");
            var stringWithUTF8 = ObjC.RegisterName("stringWithUTF8String:");
            var titlePtr = Marshal.StringToHGlobalAnsi(title);
            var nsTitle = ObjC.MsgSend(nsStringClass, stringWithUTF8, titlePtr);
            Marshal.FreeHGlobal(titlePtr);

            var setTitle = ObjC.RegisterName("setTitle:");
            ObjC.MsgSendVoid(Window, setTitle, nsTitle);

            // Get content view
            var contentView = ObjC.RegisterName("contentView");
            View = ObjC.MsgSend(Window, contentView);

            // Show window
            var makeKeyAndOrderFront = ObjC.RegisterName("makeKeyAndOrderFront:");
            ObjC.MsgSendVoid(Window, makeKeyAndOrderFront, IntPtr.Zero);

            // Activate app
            var activateIgnoringOtherApps = ObjC.RegisterName("activateIgnoringOtherApps:");
            ObjC.MsgSendVoid(NSApp, activateIgnoringOtherApps, true);
        }

        public void RunLoop()
        {
            var isVisible = ObjC.RegisterName("isVisible");
            var nextEvent = ObjC.RegisterName("nextEventMatchingMask:untilDate:inMode:dequeue:");
            var sendEvent = ObjC.RegisterName("sendEvent:");
            var terminate = ObjC.RegisterName("terminate:");
            var distantPast = ObjC.RegisterName("distantPast");
            var nsDateClass = ObjC.GetClass("NSDate");
            var nsStringClass = ObjC.GetClass("NSString");
            var stringUTF8 = ObjC.RegisterName("stringWithUTF8String:");
            var modePtr = Marshal.StringToHGlobalAnsi("kCFRunLoopDefaultMode");
            var nsMode = ObjC.MsgSend(nsStringClass, stringUTF8, modePtr);
            Marshal.FreeHGlobal(modePtr);
            var typeSelector = ObjC.RegisterName("type");
            var keyCodeSel = ObjC.RegisterName("keyCode");

            while (true)
            {
                bool visible = ObjC.MsgSendBool(Window, isVisible);
                if (!visible)
                {
                    ObjC.MsgSendVoid(NSApp, terminate, IntPtr.Zero);
                    break;
                }

                var date = ObjC.MsgSend(nsDateClass, distantPast);
                var evt  = ObjC.MsgSendEvent(NSApp, nextEvent, unchecked((IntPtr)ulong.MaxValue), date, nsMode, 1);

                if (evt != IntPtr.Zero)
                {
                    ulong evtType = (ulong)ObjC.MsgSend(evt, typeSelector);

                    switch (evtType)
                    {
                        case 10: // NSEventTypeKeyDown
                            long keyCode = ObjC.MsgSend(evt, keyCodeSel);
                            OnEvent?.Invoke("keydown", [new Value(keyCode)]);
                            break;

                        case 11: // NSEventTypeKeyUp
                            long keyCodeUp = ObjC.MsgSend(evt, keyCodeSel);
                            OnEvent?.Invoke("keyup", [new Value(keyCodeUp)]);
                            break;

                        case 1: // NSEventTypeLeftMouseDown
                            OnEvent?.Invoke("mousedown", [new Value(0L)]);
                            break;

                        case 2: // NSEventTypeLeftMouseUp
                            OnEvent?.Invoke("mouseup", [new Value(0L)]);
                            break;

                        case 3: // NSEventTypeRightMouseDown
                            OnEvent?.Invoke("mousedown", [new Value(1L)]);
                            break;

                        case 4: // NSEventTypeRightMouseUp
                            OnEvent?.Invoke("mouseup", [new Value(1L)]);
                            break;
                    }

                    ObjC.MsgSendVoid(NSApp, sendEvent, evt);
                }
            }
        }

        public void HandleWindowClose()
        {
            // Tell NSApp to terminate when last window closes
            var setDelegate = ObjC.RegisterName("setDelegate:");
            ObjC.MsgSendVoid(Window, setDelegate, NSApp);

            // Register window will close notification
            var nsNotificationCenterClass = ObjC.GetClass("NSNotificationCenter");
            var defaultCenter = ObjC.MsgSend(nsNotificationCenterClass, ObjC.RegisterName("defaultCenter"));

            // Override window release on close to terminate instead
            var setReleasedWhenClosed = ObjC.RegisterName("setReleasedWhenClosed:");
            ObjC.MsgSendVoid(Window, setReleasedWhenClosed, false);
        }
    }
}