using SubTrack.Common.Enums;

namespace SubTrack.Common;

/// <summary>
/// This base class defines a common result returned by services.
/// </summary>
/// <typeparam name="TValue">The type of the value of the result</typeparam>
/// <typeparam name="TErrorCode">The type of the error code of the result. It is preferred to be an enumeration.</typeparam>
[GenerateSerializer]
public class ServiceResult<TValue, TErrorCode> : VoidResultServiceResult<TErrorCode>
{
    /// <summary>
    /// The actual value of the result when successful.
    /// </summary>
    [Id(0)]
    public TValue? Value { get; set; }

    /// <summary>
    /// Create a new successful result.
    /// </summary>
    /// <param name="value">The value of the result</param>
    public ServiceResult(TValue value)
      : base((VoidResultServiceResult<TErrorCode>)null!)
    {
        Value = value;
    }

    /// <summary>
    /// Create a new failed result.
    /// </summary>
    /// <param name="errorCode">The code of the error</param>
    /// <param name="message">Optionally the human readable error message</param>
    /// <param name="exception">Optionally the related exception that may have been thrown from the service</param>
    public ServiceResult(TErrorCode errorCode, string? message = null, Exception? exception = null)
        : base(errorCode, message, exception)
    {
    }

    /// <summary>
    /// Create a new service result based n an existing service result
    /// </summary>
    /// <param name="result">Existing service result</param>
    public ServiceResult(ServiceResult<TValue, TErrorCode> result)
        : base(result.Error!)
    {
        Value = result.Value;
    }

    /// <summary>
    /// Create a new error service result
    /// </summary>
    /// <param name="error">Error info</param>
    public ServiceResult(ServiceResultError<TErrorCode> error)
        : base(error)
    {
    }
}

[GenerateSerializer]
public class ServiceResult<TValue> : ServiceResult<TValue, ServiceErrorCode>
{
    /// <summary>
    /// Create a new successful result.
    /// </summary>
    /// <param name="value">The value of the result</param>
    public ServiceResult(TValue value)
        : base(value)
    {

    }

    /// <summary>
    /// Create a new failed result.
    /// </summary>
    /// <param name="errorCode">The code of the error</param>
    /// <param name="message">Optionally the human readable error message</param>
    /// <param name="exception">Optionally the related exception that may have been thrown from the service</param>
    public ServiceResult(ServiceErrorCode errorCode, string? message = null, Exception? exception = null)
        : base(errorCode, message, exception)
    {

    }

    /// <summary>
    /// Create a new service result based n an existing service result
    /// </summary>
    /// <param name="result">Existing service result</param>
    public ServiceResult(ServiceResult<TValue, ServiceErrorCode> result) :
        base(result)
    {
        Value = result.Value;
        Error = result.Error;
    }

    /// <summary>
    /// Create a new error service result
    /// </summary>
    /// <param name="error">Error info</param>
    public ServiceResult(ServiceResultError<ServiceErrorCode> error) :
        base(error)
    {
        Error = error;
    }

    public static implicit operator ServiceResult<TValue>(TValue value) => new(value);
    public static implicit operator ServiceResult<TValue>(ServiceResultError<ServiceErrorCode> error) => new(error);
    public static implicit operator ServiceResult<TValue>(ServiceErrorCode errorCode) => new(new ServiceResultError<ServiceErrorCode>(errorCode));
}
