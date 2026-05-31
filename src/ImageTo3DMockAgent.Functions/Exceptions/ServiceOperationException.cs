using System.Net;

namespace ImageTo3DMockAgent.Functions.Exceptions;

public sealed class ServiceOperationException : Exception
{
    public ServiceOperationException(HttpStatusCode statusCode, string errorCode, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }

    public HttpStatusCode StatusCode { get; }

    public string ErrorCode { get; }
}
