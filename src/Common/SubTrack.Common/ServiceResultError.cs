namespace SubTrack.Common;

[GenerateSerializer]
public class ServiceResultError<TErrorCode>
{
    /// <summary>
    /// This is the code to define this error.
    /// </summary>
    [Id(0)]
    public TErrorCode? Code { get; set; }

    /// <summary>
    /// This is the human readable message of the error.
    /// </summary>
    [Id(1)]
    public string? Message { get; set; }

    /// <summary>
    /// This is the related exception that may have been thrown from the service.
    /// </summary>
    [Id(2)]
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
