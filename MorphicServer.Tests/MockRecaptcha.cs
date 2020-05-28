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
#pragma warning disable 1998
        public async Task<bool> ReCaptchaPassed(string gRecaptchaResponse)
#pragma warning restore 1998
        {
            return true;
        }
    }
}