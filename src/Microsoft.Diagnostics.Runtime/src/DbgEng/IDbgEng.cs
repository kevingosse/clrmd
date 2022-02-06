namespace Microsoft.Diagnostics.Runtime.DbgEng
{
    public interface IDbgEng
    {
        DebugClient Client { get;}
        DebugControl Control {get;}
        DebugDataSpaces DataSpaces { get; }
        DebugAdvanced Advanced { get; }
        DebugSymbols Symbols { get; }
        DebugSystemObjects SystemObjects { get; }
    }
}
