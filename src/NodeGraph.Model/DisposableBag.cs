using System.Buffers;

namespace NodeGraph.Model;

public struct DisposableBag(int capacity) : IDisposable
{
    private IDisposable[]? _disposables = ArrayPool<IDisposable>.Shared.Rent(capacity);
    private bool _isDisposed = false;
    private int _count = 0;

    public void Add(IDisposable item)
    {
        if (_isDisposed)
        {
            item.Dispose();
        }
        else
        {
            if (_disposables == null)
            {
                _disposables = ArrayPool<IDisposable>.Shared.Rent(capacity);   
            }
            if (_count >= _disposables.Length)
            {
                ArrayPool<IDisposable>.Shared.Return(_disposables);
                _disposables = ArrayPool<IDisposable>.Shared.Rent(_count * 2);
            }
            _disposables[_count++] = item;
        }
    }

    public void Clear()
    {
        if (_disposables == null)
            return;
        
        for (var i = 0; i < _count; i++)
        {
            _disposables[i].Dispose();
        }
        ArrayPool<IDisposable>.Shared.Return(_disposables);
        _disposables = null;
        _count = 0;
    }
    
    public void Dispose()
    {
        Clear();
        _isDisposed = true;
    }
}

public static class DisposableBagExtensions
{
    public static void AddTo<T>(this T item, ref DisposableBag bag) where T : IDisposable => bag.Add(item);
}