namespace Morphic.Server.Email
{
    public static class EmailConstants
    {
        public enum EmailTypes
        {
            None = 0,
            WelcomeEmailValidation = 1,
            PasswordReset = 2,
            PasswordResetEmailNotValidated = 3,
            PasswordResetUnknownEmail = 4,
            ChangePasswordEmail = 5,
            CommunityInvitation = 6
        }
    }
}