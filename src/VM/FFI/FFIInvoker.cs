using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using JSONScript.Runtime;

namespace JSONScript.VM.FFI
{
    public static unsafe class FFIInvoker
    {
        private static readonly Dictionary<string, IntPtr> loadedLibs = new();

        private static IntPtr LoadLib(string path)
        {
            if (loadedLibs.TryGetValue(path, out var handle))
                return handle;

            handle = NativeLibrary.Load(path);
            loadedLibs[path] = handle;
            return handle;
        }

        public static Value Invoke(string lib, string symbol, (string type, Value val)[] args, string returnType)
        {
            var libHandle = LoadLib(lib);
            var fnPtr = NativeLibrary.GetExport(libHandle, symbol);

            nint result = Dispatch(fnPtr, args);
            return MarshalReturn(returnType, result);
        }

        private static nint Dispatch(nint fnPtr, (string type, Value val)[] args)
        {
            //separate args into integer/pointer slots and double slots
            //on ARM64: integer args go in x0-x7, double args go in d0-d7
            //they are tracked independently and doubles don't consume integer slots
            //AAAAA
            var intArgs = new List<nint>();
            var dblArgs = new List<double>();

            //need to know which args are doubles to pick the right delegate signature
            var argTypes = new List<bool>(); //true = double, false = nint

            foreach (var (type, val) in args)
            {
                switch (type)
                {
                    case "float64":
                    case "double":
                        dblArgs.Add(val.AsDouble);
                        argTypes.Add(true);
                        break;
                    case "float32":
                    case "float":
                        dblArgs.Add(val.AsDouble);
                        argTypes.Add(true);
                        break;
                    default:
                        intArgs.Add(MarshalIntArg(type, val));
                        argTypes.Add(false);
                        break;
                }
            }

            int ic = intArgs.Count;
            int dc = dblArgs.Count;

            //pad arrays for safety
            while (intArgs.Count < 8) intArgs.Add(0);
            while (dblArgs.Count < 8) dblArgs.Add(0.0);

            nint i0 = intArgs[0], i1 = intArgs[1], i2 = intArgs[2], i3 = intArgs[3];
            nint i4 = intArgs[4], i5 = intArgs[5], i6 = intArgs[6], i7 = intArgs[7];
            double d0 = dblArgs[0], d1 = dblArgs[1], d2 = dblArgs[2], d3 = dblArgs[3];
            double d4 = dblArgs[4], d5 = dblArgs[5], d6 = dblArgs[6], d7 = dblArgs[7];

            //pick the right delegate based on int count and double count
            //format: i{intCount}d{doubleCount}
            return (ic, dc) switch
            {
                (0, 0) => ((delegate* unmanaged<nint>)fnPtr)(),
                (1, 0) => ((delegate* unmanaged<nint, nint>)fnPtr)(i0),
                (2, 0) => ((delegate* unmanaged<nint, nint, nint>)fnPtr)(i0, i1),
                (3, 0) => ((delegate* unmanaged<nint, nint, nint, nint>)fnPtr)(i0, i1, i2),
                (4, 0) => ((delegate* unmanaged<nint, nint, nint, nint, nint>)fnPtr)(i0, i1, i2, i3),
                (5, 0) => ((delegate* unmanaged<nint, nint, nint, nint, nint, nint>)fnPtr)(i0, i1, i2, i3, i4),
                (6, 0) => ((delegate* unmanaged<nint, nint, nint, nint, nint, nint, nint>)fnPtr)(i0, i1, i2, i3, i4, i5),
                (7, 0) => ((delegate* unmanaged<nint, nint, nint, nint, nint, nint, nint, nint>)fnPtr)(i0, i1, i2, i3, i4, i5, i6),
                (8, 0) => ((delegate* unmanaged<nint, nint, nint, nint, nint, nint, nint, nint, nint>)fnPtr)(i0, i1, i2, i3, i4, i5, i6, i7),

                // 2 int args (receiver + selector) + doubles (CGRect fields)
                (2, 1) => ((delegate* unmanaged<nint, nint, double, nint>)fnPtr)(i0, i1, d0),
                (2, 2) => ((delegate* unmanaged<nint, nint, double, double, nint>)fnPtr)(i0, i1, d0, d1),
                (2, 3) => ((delegate* unmanaged<nint, nint, double, double, double, nint>)fnPtr)(i0, i1, d0, d1, d2),
                (2, 4) => ((delegate* unmanaged<nint, nint, double, double, double, double, nint>)fnPtr)(i0, i1, d0, d1, d2, d3),

                // 3 int args (receiver + selector + 1 nint) + doubles
                (3, 1) => ((delegate* unmanaged<nint, nint, nint, double, nint>)fnPtr)(i0, i1, i2, d0),
                (3, 2) => ((delegate* unmanaged<nint, nint, nint, double, double, nint>)fnPtr)(i0, i1, i2, d0, d1),
                (3, 3) => ((delegate* unmanaged<nint, nint, nint, double, double, double, nint>)fnPtr)(i0, i1, i2, d0, d1, d2),
                (3, 4) => ((delegate* unmanaged<nint, nint, nint, double, double, double, double, nint>)fnPtr)(i0, i1, i2, d0, d1, d2, d3),

                // 4 int args + doubles
                (4, 1) => ((delegate* unmanaged<nint, nint, nint, nint, double, nint>)fnPtr)(i0, i1, i2, i3, d0),
                (4, 2) => ((delegate* unmanaged<nint, nint, nint, nint, double, double, nint>)fnPtr)(i0, i1, i2, i3, d0, d1),
                (4, 3) => ((delegate* unmanaged<nint, nint, nint, nint, double, double, double, nint>)fnPtr)(i0, i1, i2, i3, d0, d1, d2),
                (4, 4) => ((delegate* unmanaged<nint, nint, nint, nint, double, double, double, double, nint>)fnPtr)(i0, i1, i2, i3, d0, d1, d2, d3),

                (5, 4) => ((delegate* unmanaged<nint, nint, double, double, double, double, nint, nint, nint, nint>)fnPtr)(i0, i1, d0, d1, d2, d3, i2, i3, i4),

                _ => throw new Exception($"FFI: unsupported arg combination int={ic} double={dc}")
            };
        }

        private static nint MarshalIntArg(string type, Value val)
        {
            return type switch
            {
                "*void" or "ptr" => val.PointerValue,
                "*int" => val.PointerValue,
                "*float" => val.PointerValue,
                "int" => (nint)val.AsInt,
                "int32" => (int)val.AsInt,
                "uint" => (nint)(uint)(int)val.AsInt,
                "int64" => (nint)val.AsInt,
                "bool" => val.BoolValue ? 1 : 0,
                "string" => Marshal.StringToHGlobalAnsi(val.StringValue!),
                _ when type.StartsWith("*") => val.PointerValue,
                _                 => (nint)val.AsInt
            };
        }

        private static Value MarshalReturn(string returnType, nint result)
        {
            return returnType switch
            {
                "void" => new Value(),
                "int" => new Value(result),
                "int32" => new Value((int)result),
                "bool" => new Value(result != 0),
                "float" => new Value((double)BitConverter.Int32BitsToSingle((int)result)),
                "float64" => new Value((double)BitConverter.Int64BitsToDouble(result)),
                "string" => new Value(Marshal.PtrToStringAnsi(result) ?? ""),
                _ when returnType.StartsWith("*") => new Value(result, returnType.Substring(1)),
                _         => new Value(result, "void")
            };
        }
    }
}