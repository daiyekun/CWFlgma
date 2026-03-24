using System;

namespace CWFlgma.Infrastructure.Common;

/// <summary>
/// 雪花算法ID生成器
/// 64位ID结构：
/// 1位符号位（0）+ 41位时间戳 + 10位机器ID + 12位序列号
/// </summary>
public class SnowflakeIdGenerator
{
    private const long Twepoch = 1609459200000L; // 2021-01-01 00:00:00 UTC
    
    private const int WorkerIdBits = 10;
    private const int SequenceBits = 12;
    
    private const long MaxWorkerId = -1L ^ (-1L << WorkerIdBits);
    private const long MaxSequence = -1L ^ (-1L << SequenceBits);
    
    private const int WorkerIdShift = SequenceBits;
    private const int TimestampLeftShift = SequenceBits + WorkerIdBits;
    
    private readonly long _workerId;
    private long _sequence;
    private long _lastTimestamp = -1L;
    
    private static readonly object Lock = new();
    private static SnowflakeIdGenerator? _instance;
    
    public static SnowflakeIdGenerator Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (Lock)
                {
                    _instance ??= new SnowflakeIdGenerator(GetDefaultWorkerId());
                }
            }
            return _instance;
        }
    }
    
    public SnowflakeIdGenerator(long workerId)
    {
        if (workerId > MaxWorkerId || workerId < 0)
        {
            throw new ArgumentException($"Worker ID must be between 0 and {MaxWorkerId}");
        }
        _workerId = workerId;
    }
    
    public long NextId()
    {
        lock (Lock)
        {
            var timestamp = GetCurrentTimestamp();
            
            if (timestamp < _lastTimestamp)
            {
                throw new InvalidOperationException("Clock moved backwards");
            }
            
            if (timestamp == _lastTimestamp)
            {
                _sequence = (_sequence + 1) & MaxSequence;
                if (_sequence == 0)
                {
                    timestamp = WaitNextMillis(_lastTimestamp);
                }
            }
            else
            {
                _sequence = 0;
            }
            
            _lastTimestamp = timestamp;
            
            return ((timestamp - Twepoch) << TimestampLeftShift)
                   | (_workerId << WorkerIdShift)
                   | _sequence;
        }
    }
    
    private static long WaitNextMillis(long lastTimestamp)
    {
        var timestamp = GetCurrentTimestamp();
        while (timestamp <= lastTimestamp)
        {
            timestamp = GetCurrentTimestamp();
        }
        return timestamp;
    }
    
    private static long GetCurrentTimestamp()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
    
    private static long GetDefaultWorkerId()
    {
        // 使用机器名的哈希值作为默认WorkerId
        var machineName = Environment.MachineName;
        var hash = Math.Abs(machineName.GetHashCode());
        return hash % (MaxWorkerId + 1);
    }
}

/// <summary>
/// ID生成器扩展方法
/// </summary>
public static class IdGeneratorExtensions
{
    private static readonly SnowflakeIdGenerator Generator = SnowflakeIdGenerator.Instance;
    
    /// <summary>
    /// 生成新的雪花算法ID
    /// </summary>
    public static long NewId()
    {
        return Generator.NextId();
    }
}
