# Email Sending Subsystem

## Metrics

See top-level Metrics document.

## Settings

    "EmailSettings": {
        "Type": "sendgrid | sendinblue | disabled"  (or "log" for development)
        "EmailFromAddress": "From address to use when sending email",
        "EmailFromFullname": "From name to use when sending email", 
        "SendgridSettings": {
          "ApiKey": "api key for SendGrid"
          "WelcomeEmailValidationId": "<template Id>",
          "PasswordResetId": "<template Id>",
          "PasswordResetEmailNotValidatedId": "<template Id>",
          "PasswordResetUnknownEmailId": "<template Id>",
          "ChangePasswordEmailId": "<template Id>",
          "CommunityInvitationId": "<template Id>"
        },
        "SendInBlueSettings": {
          "ApiKey": "api key for SendInBlue"
          "WelcomeEmailValidationId": "<template Id>",
          "PasswordResetId": "<template Id>",
          "PasswordResetEmailNotValidatedId": "<template Id>",
          "PasswordResetUnknownEmailId": "<template Id>",
          "ChangePasswordEmailId": "<template Id>",
          "CommunityInvitationId": "<template Id>"
        }
    }

# Hangfire information

Email sending flows through the Hangfire Background processing system. It has a console enabled
on `/hangfire` (try http://localhost:5002/hangfire), but it's not exposed outside of the kubernetes pod (because it's not part of `/v1` which is all
we route for). In addition, Hangfire itself prevents access from the outside.

To access the Hangfire console in kubernetes `kubectl port-forward` can be used, if you have kubernetes
access:

    kubectl -n morphiclite port-forward svc/v0-morphic-lite 8000:80

then access http://localhost:8000/hangfire
