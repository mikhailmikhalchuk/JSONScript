using System;
using System.Runtime.InteropServices;

namespace JSONScript.VM.Graphics.Metal
{
    public static partial class ObjC
    {
        /// <summary>
        /// Returns the class of an object.
        /// </summary>
        /// <param name="name">The object you want to inspect.</param>
        /// <returns>The class object of which object is an instance, or Nil if object is nil.</returns>
        [LibraryImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_getClass", StringMarshalling = StringMarshalling.Utf8)]
        internal static partial IntPtr GetClass(string name);

        /// <summary>
        /// Registers a method with the Objective-C runtime system, maps the method name to a selector, and returns the selector value.
        /// </summary>
        /// <param name="name">A pointer to a C string. Pass the name of the method you wish to register.</param>
        /// <returns>A pointer of type SEL specifying the selector for the named method.</returns>
        [LibraryImport("/usr/lib/libobjc.dylib", EntryPoint = "sel_registerName", StringMarshalling = StringMarshalling.Utf8)]
        internal static partial IntPtr RegisterName(string name);

        /// <summary>
        /// Sends a message with a simple return value to an instance of a class.
        /// </summary>
        /// <param name="receiver">A pointer that points to the instance of the class that is to receive the message.</param>
        /// <param name="selector">The selector of the method that handles the message.</param>
        /// <returns>The return value of the method.</returns>
        [LibraryImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        internal static partial IntPtr MsgSend(IntPtr receiver, IntPtr selector);

        /// <inheritdoc cref="MsgSend(IntPtr, IntPtr)"/>
        /// <remarks>Overload: (receiver, selector, IntPtr)</remarks>
        [LibraryImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        internal static partial IntPtr MsgSend(IntPtr receiver, IntPtr selector, IntPtr arg1);

        /// <inheritdoc cref="MsgSend(IntPtr, IntPtr)"/>
        /// <remarks>Overload: (receiver, selector, IntPtr, IntPtr)</remarks>
        [LibraryImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        internal static partial IntPtr MsgSend(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2);

        /// <inheritdoc cref="MsgSend(IntPtr, IntPtr)"/>
        /// <remarks>Overload: (receiver, selector, IntPtr, double)</remarks>
        [LibraryImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        internal static partial IntPtr MsgSend(IntPtr receiver, IntPtr selector, double arg1);

        /// <inheritdoc cref="MsgSend(IntPtr, IntPtr)"/>
        /// <remarks>Overload: (receiver, selector, CGRect)</remarks>
        [LibraryImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        internal static partial IntPtr MsgSend(IntPtr receiver, IntPtr selector, CGRect arg1);

        /// <inheritdoc cref="MsgSend(IntPtr, IntPtr)"/>
        /// <remarks>Overload: (receiver, selector, CGRect, IntPtr, IntPtr, IntPtr)</remarks>
        [LibraryImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        internal static partial IntPtr MsgSend(IntPtr receiver, IntPtr selector, CGRect arg1, IntPtr arg2, IntPtr arg3, IntPtr arg4);

        /// <inheritdoc cref="MsgSend(IntPtr, IntPtr)"/>
        /// <remarks>Overload: (receiver, selector, UIntPtr)</remarks>
        [LibraryImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        internal static partial IntPtr MsgSend(IntPtr receiver, IntPtr selector, UIntPtr index);

        /// <inheritdoc cref="MsgSend(IntPtr, IntPtr)"/>
        /// <remarks>Overload: (receiver, selector, IntPtr, IntPtr, IntPtr)</remarks>
        [LibraryImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        internal static partial IntPtr MsgSend(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2, IntPtr arg3);

        /// <summary>
        /// Sends a message with a simple return value to an instance of a class.
        /// </summary>
        /// <param name="receiver">A pointer that points to the instance of the class that is to receive the message.</param>
        /// <param name="selector">The selector of the method that handles the message.</param>
        [LibraryImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        internal static partial void MsgSendVoid(IntPtr receiver, IntPtr selector);

        /// <inheritdoc cref="MsgSendVoid(IntPtr, IntPtr)"/>
        /// <remarks>Overload: (receiver, selector, IntPtr)</remarks>
        [LibraryImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        internal static partial void MsgSendVoid(IntPtr receiver, IntPtr selector, IntPtr arg1);

        /// <inheritdoc cref="MsgSendVoid(IntPtr, IntPtr)"/>
        /// <remarks>Overload: (receiver, selector, bool)</remarks>
        [LibraryImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        internal static partial void MsgSendVoid(IntPtr receiver, IntPtr selector, [MarshalAs(UnmanagedType.Bool)] bool arg1);

        /// <inheritdoc cref="MsgSendVoid(IntPtr, IntPtr)"/>
        /// <remarks>Overload: (receiver, selector, IntPtr, IntPtr, UIntPtr)</remarks>
        [LibraryImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        internal static partial void MsgSendVoid(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2, UIntPtr arg3);

        /// <inheritdoc cref="MsgSendVoid(IntPtr, IntPtr)"/>
        /// <remarks>Overload: (receiver, selector, CGSize)</remarks>
        [LibraryImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        internal static partial void MsgSendVoid(IntPtr receiver, IntPtr selector, CGSize arg1);

        /// <summary>
        /// Sends a message with a simple return value to an instance of a class.
        /// </summary>
        /// <param name="receiver">A pointer that points to the instance of the class that is to receive the message.</param>
        /// <param name="selector">The selector of the method that handles the message.</param>
        /// <returns>The return value of the method, marshaled as a bool.</returns>
        [LibraryImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool MsgSendBool(IntPtr receiver, IntPtr selector);

        [LibraryImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        internal static partial IntPtr MsgSendIndex(IntPtr receiver, IntPtr selector, ulong index);

        [LibraryImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        internal static partial IntPtr MsgSendEvent(IntPtr receiver, IntPtr selector, IntPtr mask, IntPtr date, IntPtr mode, IntPtr dequeue);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CGPoint
    {
        public double X;
        public double Y;
        public CGPoint(double x, double y)
        {
            X = x;
            Y = y;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CGSize
    {
        public double Width;
        public double Height;
        public CGSize(double w, double h)
        {
            Width = w;
            Height = h;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CGRect
    {
        public CGPoint Origin;
        public CGSize Size;
        public CGRect(double x, double y, double w, double h)
        {
            Origin = new CGPoint(x, y);
            Size = new CGSize(w, h);
        }
    }
}