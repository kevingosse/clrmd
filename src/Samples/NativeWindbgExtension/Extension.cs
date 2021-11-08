using Microsoft.Diagnostics.Runtime;
using Microsoft.Diagnostics.Runtime.DbgEng;
using Microsoft.Diagnostics.Runtime.Interop;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace NativeWindbgExtension
{
    public class Extension
    {
        private static readonly RefCountedFreeLibrary _library = new RefCountedFreeLibrary(IntPtr.Zero);

        private static DebugControl DebugControl { get; set; }
        public static DataTarget DataTarget { get; private set; }
        public static ClrRuntime Runtime { get; private set; }

        [UnmanagedCallersOnly(EntryPoint = "DebugExtensionInitialize")]
        public static int DebugExtensionInitialize(ref uint version, ref uint flags)
        {
            version = (1 & 0xffff) << 16;
            flags = 0;
            return 0;
        }

        [UnmanagedCallersOnly(EntryPoint = "helloworld")]
        public static void HelloWorld(IntPtr client, IntPtr argsPtr)
        {
            var args = Marshal.PtrToStringAnsi(argsPtr);

            if (!InitApi(client))
            {
                return;
            }

            Console.WriteLine("Top 10 objects on the heap: ");

            foreach (var obj in Runtime.Heap.EnumerateObjects().Take(10))
            {
                Console.WriteLine("{0:x2}: {1}", obj.Address, obj.Type.ToString());
            }
        }

        private static bool InitApi(IntPtr ptrClient)
        {
            // On our first call to the API:
            //   1. Store a copy of IDebugClient in DebugClient.
            //   2. Replace Console's output stream to be the debugger window.
            //   3. Create an instance of DataTarget using the IDebugClient.
            if (DebugControl == null)
            {
                var systemObjects = new DebugSystemObjects(_library, ptrClient);
                DebugControl = new DebugControl(_library, ptrClient, systemObjects);

                var stream = new StreamWriter(new DebugEngineStream(DebugControl));
                stream.AutoFlush = true;
                Console.SetOut(stream);

                DataTarget = DataTarget.CreateFromDbgEng(ptrClient);
            }

            // If our ClrRuntime instance is null, it means that this is our first call, or
            // that the dac wasn't loaded on any previous call.  Find the dac loaded in the
            // process (the user must use .cordll), then construct our runtime from it.
            if (Runtime == null)
            {
                try
                {
                    Runtime = DataTarget.ClrVersions.First().CreateRuntime();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                // Just find a module named mscordacwks and assume it's the one the user
                // loaded into windbg.
                //using (var process = Process.GetCurrentProcess())
                //{
                //    foreach (ProcessModule module in process.Modules)
                //    {
                //        var fileName = module.FileName.ToLowerInvariant();

                //        if (fileName.Contains("mscordacwks") || fileName.Contains("mscordaccore"))
                //        {
                //            Runtime = DataTarget.ClrVersions.Single().CreateRuntime(module.FileName);
                //            break;
                //        }
                //    }
                //}

                // Otherwise, the user didn't run .cordll.
                if (Runtime == null)
                {
                    Console.WriteLine("Mscordacwks.dll not loaded into the debugger.");
                    Console.WriteLine("Run .cordll to load the dac before running this command.");
                }
            }
            else
            {
                // If we already had a runtime, flush it for this use.  This is ONLY required
                // for a live process or iDNA trace.  If you use the IDebug* apis to detect
                // that we are debugging a crash dump you may skip this call for better perf.
                Runtime.FlushCachedData();
            }

            return Runtime != null;
        }
    }
}
