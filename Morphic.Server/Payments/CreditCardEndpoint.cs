using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Morphic.Server.Payments
{
    using Http;
    using Security;
    
    [Path("/v1/users/{UserId}/creditcards")]
    public class CreditCardEndpoint : Endpoint

    {
        public CreditCardEndpoint(IHttpContextAccessor contextAccessor, ILogger<Endpoint> logger) : base(contextAccessor, logger)
        {
        }
        
        public class CreditCardDetails
        {
            public string ProcessorCreditCardId;
            public DateTime CreatedUtc;
            public DateTime UpdatedUtc;
            public EncryptedString Last4;
            public string Name;
        }

    }
    
    
}