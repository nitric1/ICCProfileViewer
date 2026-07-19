using System;
using System.Collections.Generic;
using lcmsNET;

namespace ICCProfileViewer.Lcms;

internal sealed class LcmsOperationContext : IDisposable
{
    private readonly object errorSyncRoot = new();
    private readonly List<LcmsError> errors = [];
    private readonly ErrorHandler errorHandler;

    public LcmsOperationContext()
    {
        errorHandler = OnError;
        Context = Context.Create(nint.Zero, nint.Zero);
        Context.SetErrorHandler(errorHandler);
    }

    public Context Context { get; }

    public IReadOnlyList<LcmsError> GetErrors()
    {
        lock (errorSyncRoot)
        {
            return errors.ToArray();
        }
    }

    public void Dispose()
    {
        Context.Dispose();
        GC.KeepAlive(errorHandler);
    }

    private void OnError(nint contextId, int errorCode, string errorText)
    {
        lock (errorSyncRoot)
        {
            errors.Add(new LcmsError(errorCode, errorText));
        }
    }
}
