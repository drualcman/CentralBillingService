using System.Runtime.CompilerServices;

namespace Aboitiz.Power.MobileAp.Core.Data.Diagnostics;

public interface IRequestFlowTracker : IAsyncDisposable
{
#nullable enable
    string? CurrentOperationId { get; }

    void Start(
        string? operationId = null,
        object? data = null,
        bool overwriteCurrent = false,
        [CallerMemberName] string methodName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0);

    void Track(
        string message,
        object? data = null,
        string? operationId = null,
        [CallerMemberName] string methodName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0);

    void TrackException(
        Exception exception,
        object? data = null,
        string? operationId = null,
        [CallerMemberName] string methodName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0);

    Guid QueryStart(
        string queryName,
        object? data = null,
        string? operationId = null,
        [CallerMemberName] string methodName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0);

    void QueryEnd(
        Guid queryId,
        object? data = null,
        string? operationId = null,
        [CallerMemberName] string methodName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0);

    void End(
        object? data = null,
        string? operationId = null,
        [CallerMemberName] string methodName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0);
}