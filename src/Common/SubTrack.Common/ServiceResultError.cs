namespace SubTrack.Common;

public class ServiceResultError<TErrorCode>
{
    /// <summary>
    /// This is the code to define this error.
    /// </summary>
    public TErrorCode? Code { get; set; }

    /// <summary>
    /// This is the human readable message of the error.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// This is the related exception that may have been thrown from the service.
    /// </summary>
    public Exception? Exception { get; set; }

    public ServiceResultError()
    {

    }

    public ServiceResultError(TErrorCode code, string? message = null, Exception? exception = null)
    {
        Code = code;
        Message = message;
        Exception = exception;
    }
}
