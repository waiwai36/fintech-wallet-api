namespace wallet.Exceptions
{
    public class AppException : Exception
    {
        public int StatusCode { get; }

        public AppException(string message, int statusCode = StatusCodes.Status500InternalServerError)
            : base(message)
        {
            StatusCode = statusCode;
        }
    }
}
