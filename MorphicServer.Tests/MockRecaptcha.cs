using System.Threading.Tasks;

namespace MorphicServer.Tests
{
    public class MockRecaptcha : IRecaptcha
    {
        public string Key
        {
            get
            {
                return "MockRecaptchaKey";
            }
        }
        
        public Task<bool> ReCaptchaPassed(string action, string gRecaptchaResponse)
        {
            return Task.FromResult(true);
        }
    }
}