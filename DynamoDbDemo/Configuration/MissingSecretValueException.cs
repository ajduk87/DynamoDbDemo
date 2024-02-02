
using System;
using System.Net;
using System.Runtime.Serialization;

namespace DynamoDbDemo.Configuration
{
    [Serializable]
    public class MissingSecretValueException : Exception
    {
        public string SecretName { get; set; } = null;


        public string SecretArn { get; set; } = null;


        public HttpStatusCode StatusCode { get; set; }

        public MissingSecretValueException(string errorMessage, string secretName, string secretArn, Exception exception, HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
            : base(errorMessage, exception)
        {
            SecretName = secretName ?? throw new ArgumentNullException("secretName");
            SecretArn = secretArn ?? throw new ArgumentNullException("secretArn");
            StatusCode = statusCode;
        }

        protected MissingSecretValueException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
