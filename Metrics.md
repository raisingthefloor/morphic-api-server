# Metrics

All metrics can be searched, graphed, and alerted on in the Grafana server.

## Prometheus Metrics

### Histograms
Most (including our) Prometheus drivers convert a single histogram metric is converted into three actual metrics. Take for example
a metric `email_send_duration`:

* email_send_duration_bucket
  * A breakdown (by buckets) of how long email sending took
* email_send_duration_count
  * A count of emails sent
* email_send_duration_sum
  * the sum of the duration for emails. Used with `email_send_duration_count` to get an average, for example.

### Counter

A simple counter, increasing always.

### Gauge

An integer count which can go up and down

## Morphic Server Metrics

### encryption_duration
* Description: The time in seconds (fractional) it took the encrypt something
* Type: histogram (bucket, count, sum)
* Labels:
  * `cipher`: name of the cipher used

### decryption_duration
* Description: The time in seconds (fractional) it took the decrypt something
* Type: histogram (bucket, count, sum)
* Labels:
  * `cipher`: name of the cipher used

### morphic_bad_user_auth
* Description: A count of bad user authentications
* Type: Counter
* Labels:
  * `type`: "EmptyUsername", "CredentialNotFound", "UserLockedOut", "InvalidPassword", "UserNotFound"

### email_send_duration
* Description: The time in seconds (fractional) it took to send an email.
* Type: histogram (bucket, count, sum)
* Labels:
  * `type`: WelcomeEmailValidation, PasswordReset, PasswordResetEmailNotValidated, PasswordResetUnknownEmail, ChangePasswordEmail, CommunityInvitation
  * `destination`: sendgrid, sendinblue
  * `success`: true or false

### http_server_requests
* Description: A count of http requests served
* Type: Counter
* Labels:
  * `path`: url path (templated)
  * `method`: GET, POST, PUT, etc
  * `status`: the return status. 200, 302, 404, etc.

### http_server_requests_duration
* Description: The time in seconds (fractional) it took to serve an http request
* Type: histogram (bucket, count, sum)
* Labels:
  * `path`: url path (templated)
  * `method`: GET, POST, PUT, etc
  * `status`: the return status. 200, 302, 404, etc.

### hangfire_job_duration
* Description: The time in seconds (fractional) it took a hangfire background job to run
* Type: histogram (bucket, count, sum)
* Labels:
  * `jobName`: the name of the job run
  * `status`: the hangfile state name

### hangfire_job_retry_count
* Description: The number of retries for hangfire background jobs
* Type: Counter
* Labels:
  * `jobName`: the name of the job run

### logs_log_level_count
* Description: A count of logs
* Type: Counter
* Labels:
  * `level`: log level

## Kubernetes Labels

Prometheus scraping adds more labels for various kubernetes values.

        email_send_duration_count{app="morphic-lite",destination="sendgrid",instance="192.168.32.188:80",job="kubernetes-pods",kubernetes_namespace="morphiclite",kubernetes_pod_name="v0-morphic-lite-d6778d7bf-55mf5",pod_template_hash="d6778d7bf",success="True",type="EmailValidation"}
        email_send_duration_count{app="morphic-lite",destination="sendgrid",instance="192.168.44.240:80",job="kubernetes-pods",kubernetes_namespace="morphiclite",kubernetes_pod_name="v0-morphic-lite-7df579c68c-gh5wr",pod_template_hash="7df579c68c",success="True",type="EmailValidation"}
        email_send_duration_count{app="morphic-lite",destination="sendgrid",instance="192.168.44.240:80",job="kubernetes-pods",kubernetes_namespace="morphiclite",kubernetes_pod_name="v0-morphic-lite-8ff67588-9mjwj",pod_template_hash="8ff67588",success="True",type="EmailValidation"}
        email_send_duration_count{app="morphic-lite",destination="sendgrid",instance="192.168.61.171:80",job="kubernetes-pods",kubernetes_namespace="morphiclite",kubernetes_pod_name="v0-morphic-lite-55647ddfcb-7fhbd",pod_template_hash="55647ddfcb",success="True",type="PasswordResetNoUser"}
