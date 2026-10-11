using SubTrack.Common.Enums;

namespace SubTrack.Common;

[GenerateSerializer]
public class VoidResultServiceResult<TErrorCode>
{
    /// <summary>
    /// The error details when the result has failed.
    /// </summary>
    [Id(0)]
    public ServiceResultError<TErrorCode>? Error { get; set; }

    /// <summary>
    /// Returns true when the result is successful.
    /// </summary>
    public bool Success => Error == null;

    /// <summary>
    /// Returns true when the result is failure.
    /// </summary>
    public bool Failed => Error is { };

    public VoidResultServiceResult()
    {
        Error = null;
    }

    /// <summary>
    /// Create a new successful result.
    /// </summary>
    /// <param name="value">The value of the result</param>
    public VoidResultServiceResult(TErrorCode value)
    {
        Error = new ServiceResultError<TErrorCode>(value);
    }

    /// <summary>
    /// Create a new failed result.
    /// </summary>
    /// <param name="errorCode">The code of the error</param>
    /// <param name="message">Optionally the human readable error message</param>
    /// <param name="exception">Optionally the related exception that may have been thrown from the service</param>
    public VoidResultServiceResult(TErrorCode errorCode, string? message = null, Exception? exception = null)
    {
        Error = new ServiceResultError<TErrorCode>
        {
            Code = errorCode,
            Message = message,
            Exception = exception
        };
    }

    /// <summary>
    /// Create a new service result based n an existing service result
    /// </summary>
    /// <param name="result">Existing service result</param>
    public VoidResultServiceResult(VoidResultServiceResult<TErrorCode> result)
    {
        Error = result?.Error;
    }

    /// <summary>
    /// Create a new error service result
    /// </summary>
    /// <param name="error">Error info</param>
    public VoidResultServiceResult(ServiceResultError<TErrorCode> error)
    {
        Error = error;
    }
}

[GenerateSerializer]
public class VoidResultServiceResult : VoidResultServiceResult<ServiceErrorCode>
{
    /// <summary>
    /// Create a new successful result.
    /// </summary>
    public VoidResultServiceResult()
    {
    }

    /// <summary>
    /// Create a new failed result.
    /// </summary>
    /// <param name="errorCode">The code of the error</param>
    /// <param name="message">Optionally the human readable error message</param>
    /// <param name="exception">Optionally the related exception that may have been thrown from the service</param>
    public VoidResultServiceResult(ServiceErrorCode errorCode, string? message = null, Exception? exception = null)
        : base(errorCode, message, exception)
    {
    }

    /// <summary>
    /// Create a new error service result
    /// </summary>
    /// <param name="error">Error info</param>
    public VoidResultServiceResult(ServiceResultError<ServiceErrorCode> error)
        : base(error)
    {
    }

    public static implicit operator VoidResultServiceResult(ServiceResultError<ServiceErrorCode> error) => new(error);
    public static implicit operator VoidResultServiceResult(ServiceErrorCode errorCode) => new(errorCode);
}
