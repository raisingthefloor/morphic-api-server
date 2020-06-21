# Email Sending Subsystem

## Metrics

### Metric Name

    email_send_duration
    
### Metric Labels
The following labels/tags are added to any measurement:

    {"type", "destination", "success"}
    
* type
  * the type of email, i.e. one of Morphic.Server.Email.EmailConstants
* destination
  * The email-sending-service: sendgrid, sendinblue (i.e. whatever EmailSettings.Type is set to)
* success
  * true or false whether the email sending succeeded
  
### Example

_Note that prometheus scraping adds more labels for various kubernetes values._

        email_send_duration_count{app="morphic-lite",destination="sendgrid",instance="192.168.32.188:80",job="kubernetes-pods",kubernetes_namespace="morphiclite",kubernetes_pod_name="v0-morphic-lite-d6778d7bf-55mf5",pod_template_hash="d6778d7bf",success="True",type="EmailValidation"}
        email_send_duration_count{app="morphic-lite",destination="sendgrid",instance="192.168.44.240:80",job="kubernetes-pods",kubernetes_namespace="morphiclite",kubernetes_pod_name="v0-morphic-lite-7df579c68c-gh5wr",pod_template_hash="7df579c68c",success="True",type="EmailValidation"}
        email_send_duration_count{app="morphic-lite",destination="sendgrid",instance="192.168.44.240:80",job="kubernetes-pods",kubernetes_namespace="morphiclite",kubernetes_pod_name="v0-morphic-lite-8ff67588-9mjwj",pod_template_hash="8ff67588",success="True",type="EmailValidation"}
        email_send_duration_count{app="morphic-lite",destination="sendgrid",instance="192.168.61.171:80",job="kubernetes-pods",kubernetes_namespace="morphiclite",kubernetes_pod_name="v0-morphic-lite-55647ddfcb-7fhbd",pod_template_hash="55647ddfcb",success="True",type="PasswordResetNoUser"}


### Prometheus Metrics

In Prometheus, the single metric is converted into three:

* email_send_duration_bucket
  * A breakdown (by buckets) of how long email sending took
* email_send_duration_count
  * A count of emails sent
* email_send_duration_sum
  * the sum of the duration for emails. Used with `email_send_duration_count` to get an average, for example.

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
          "ChangePasswordEmailId": "<template Id>"
        },
        "SendInBlueSettings": {
          "ApiKey": "api key for SendInBlue"
          "WelcomeEmailValidationId": "<template Id>",
          "PasswordResetId": "<template Id>",
          "PasswordResetEmailNotValidatedId": "<template Id>",
          "PasswordResetUnknownEmailId": "<template Id>",
          "ChangePasswordEmailId": "<template Id>"
        }
    }
