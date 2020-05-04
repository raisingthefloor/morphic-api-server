using System;
using System.Threading.Tasks;

namespace MorphicServer
{
    /// <summary>
    /// Class for Pending Emails.
    ///
    /// TODO Implement some background task to actually send these.
    /// </summary>
    public class PendingEmail : Record
    {
        public string UserId { get; set; }
        public string ToEmail { get; set; }
        public string FromEmail { get; set; }
        public string EmailText { get; set; }

        public PendingEmail(string userId, string to, string from, string text)
        {
            Id = Guid.NewGuid().ToString();
            UserId = userId;
            ToEmail = EncryptedField.FromPlainText(to).ToCombinedString();
            FromEmail = from; // it's us. No need to encrypt it.
            EmailText = EncryptedField.FromPlainText(text).ToCombinedString();
        }
    }

    public class EmailTemplates
    {
        // TODO i18n? localization?
        private const string EmailVerificationMsgTemplate = 
            @"Dear {0},

To verify your email Address {1} please click the following link: {2}.

Regards,

--
{3}";
        
        public static async Task NewVerificationEmail(Database db, User user, string urlTemplate)
        {
            var encrEmail = EncryptedField.FromCombinedString(user.EmailEncrypted!);

            bool isPrimary;
            var email = encrEmail.Decrypt(out isPrimary);

            string dearUser = "";
            if (user.FirstName != "" || user.LastName != "")
            {
                if (user.FirstName != "") dearUser = user.FirstName!;
                if (dearUser != "" && user.LastName != "")
                {
                    dearUser += " " + user.LastName;
                }
            }
            else
            {
                dearUser = email;
            }

            var oneTimeToken = new OneTimeToken(user.Id);
            var link = urlTemplate.Replace("{oneTimeToken}", oneTimeToken.Token);
            var from = "support@morphic.world";
            var msg = string.Format(EmailVerificationMsgTemplate,
                dearUser,
                email,
                link,
                from);
            var pending = new PendingEmail(user.Id, email, from, msg);
            await db.Save(oneTimeToken);
            await db.Save(pending);
        }
    }
}