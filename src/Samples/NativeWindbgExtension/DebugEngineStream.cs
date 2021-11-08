using Microsoft.Diagnostics.Runtime.DbgEng;
using Microsoft.Diagnostics.Runtime.Interop;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace NativeWindbgExtension
{
    internal class DebugEngineStream : Stream
    {
        private readonly DebugControl _control;

        public DebugEngineStream(DebugControl control)
        {
            _control = control;
        }

        public void Clear()
        {
            _control.Release();
        }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override void Flush()
        {
        }

        public override long Length => -1;

        public override long Position
        {
            get => 0;
            set { }
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotImplementedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();

        public override void SetLength(long value) => throw new NotImplementedException();

        public override void Write(byte[] buffer, int offset, int count)
        {
            string str = Encoding.UTF8.GetString(buffer, offset, count);
            _control.ControlledOutput((uint)(DEBUG_OUTCTL.ALL_CLIENTS | DEBUG_OUTCTL.DML), (uint)DEBUG_OUTPUT.NORMAL, str);
        }
    }
}
