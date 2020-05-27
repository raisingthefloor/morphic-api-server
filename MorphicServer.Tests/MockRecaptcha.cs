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
        public async Task<bool> ReCaptchaPassed(string gRecaptchaResponse)
        {
            return true;
        }
    }
}