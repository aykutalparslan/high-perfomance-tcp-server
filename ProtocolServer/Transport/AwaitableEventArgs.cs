using System.Net.Sockets;
using System.Threading.Tasks.Sources;

namespace ProtocolServer.Transport;

//From: https://github.com/dotnet/aspnetcore/blob/main/src/Servers/Kestrel/Transport.Sockets/src/Internal/SocketAwaitableEventArgs.cs#L10-L77
public class AwaitableEventArgs : SocketAsyncEventArgs, IValueTaskSource<int>
{
    private ManualResetValueTaskSourceCore<int> _source = new ManualResetValueTaskSourceCore<int>();
    
    public AwaitableEventArgs() :
        base(unsafeSuppressExecutionContextFlow: true)
    {
    }

    protected override void OnCompleted(SocketAsyncEventArgs args)
    {
        if (SocketError != SocketError.Success)
        {
            _source.SetException(new SocketException((int)SocketError));
        }
        _source.SetResult(BytesTransferred);
    }

    public int GetResult(short token)
    {
        int result = _source.GetResult(token);
        _source.Reset();
        return result;
    }

    public ValueTaskSourceStatus GetStatus(short token)
    {
        return _source.GetStatus(token);
    }

    public void OnCompleted(Action<object?> continuation, object? state, short token,
        ValueTaskSourceOnCompletedFlags flags)
    {
        _source.OnCompleted(continuation, state, token, flags);
    }
}