using Microsoft.Diagnostics.Runtime;
using Microsoft.Diagnostics.Runtime.DbgEng;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace NativeWindbgExtension
{
    public class Extension
    {
        public static DataTarget DataTarget { get; private set; }
        public static ClrRuntime Runtime { get; private set; }

        [UnmanagedCallersOnly(EntryPoint = "DebugExtensionInitialize")]
        public static unsafe int DebugExtensionInitialize(uint* version, uint* flags)
        {
            *version = (1 & 0xffff) << 16;
            *flags = 0;
            return 0;
        }

        [UnmanagedCallersOnly(EntryPoint = "heapstat")]
        public static void HeapStat(IntPtr client, IntPtr argsPtr)
        {
            var args = Marshal.PtrToStringAnsi(argsPtr);

            if (!InitApi(client))
            {
                return;
            }

            var heap = Runtime.Heap;

            var stats = from obj in heap.EnumerateObjects()
                        group obj by obj.Type into g
                        let size = g.Sum(p => (long)p.Size)
                        orderby size
                        select new
                        {
                            Size = size,
                            Count = g.Count(),
                            g.Key.Name
                        };

            foreach (var entry in stats)
            {
                Console.WriteLine("{0,12:n0} {1,12:n0} {2}", entry.Count, entry.Size, entry.Name);
            }

        }

        private static bool InitApi(IntPtr ptrClient)
        {
            // On our first call to the API:
            //   1. Store a copy of IDebugClient in DebugClient.
            //   2. Replace Console's output stream to be the debugger window.
            //   3. Create an instance of DataTarget using the IDebugClient.
            if (DataTarget == null)
            {
                DataTarget = DataTarget.CreateFromDbgEng(ptrClient);

                var dbgEng = (IDbgEng)DataTarget.DataReader;

                var stream = new StreamWriter(new DebugEngineStream(dbgEng.Control));
                stream.AutoFlush = true;
                Console.SetOut(stream);
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
