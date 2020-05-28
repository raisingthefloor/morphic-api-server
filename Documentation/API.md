The Morphic Lite API

Endpoints
=========

* [User Registration](#section-user-registration)
  * [`/v1/register/username`](#endpoint-register-username)
  * [`/v1/register/key`](#endpoint-register-key)
* [Authentication](#section-authentication)
  * [`/v1/auth/username`](#endpoint-auth-username)
  * [`/v1/auth/key`](#endpoint-auth-key)
* [User Data](#section-user-data)
  * [`/v1/users/{id}`](#endpoint-user)
  * [`/v1/users/{uid}/preferences/{id}`](#endpoint-preferences)
  * [`/v1/users/{id}/changePassword`](#endpoint-changepassword)
* [Password Reset](#section-password-reset)
  * [`/v1/auth/username/password_reset/{oneTimeToken}`](#endpoint-password-reset)  
  * [`/v1/auth/username/password_reset/request`](#endpoint-password-reset-request)  


<a name="section-user-registration"></a>User Registration
=================

<a name="endpoint-register-username"></a>/v1/register/username
------------------

### POST

Create a new user with empty preferences and the ability to login with the
given username/password credentials.

Immediately log the user in and return an authentication token.

<table>
  <tbody>
    <tr>
      <th colspan="4">Headers</th>
    </tr>
    <tr>
      <th><code>Content-Type</code></th>
      <td colspan="2"><code>application/json; charset=utf-8</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4">Request Body</th>
    </tr>
    <tr>
      <th><code>username</code></th>
      <td>The user-chosen username</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th><code>password</code></th>
      <td>The user-chosen password</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th><code>email</code></th>
      <td>The user's email</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th><code>first_name</code></th>
      <td>The user's first name</td>
      <td><code>String</code></td>
      <td>Optional</td>
    </tr>
    <tr>
      <th><code>last_name</code></th>
      <td>The user's last name</td>
      <td><code>String</code></td>
      <td>Optional</td>
    </tr>
    <tr>
      <th colspan="4"><code>200</code> Response Body</th>
    </tr>
    <tr>
      <th><code>token</code></th>
      <td>The token string to use in the <code>Authorization</code> header</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th><code>user</code></th>
      <td>The user object</td>
      <td><a href="#endpoint-user"><code>User</code></a></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4"><code>400</code> Response Body</th>
    </tr>
    <tr>
      <th rowspan="6"><code>error</code></th>
      <td>Missing required fields</td>
      <td colspan="2"><code>"missing_required"</code></td>
    </tr>
    <tr>
      <td>Username already exists</td>
      <td colspan="2"><code>"existing_username"</code></td>
    </tr>
    <tr>
      <td>Email already exists</td>
      <td colspan="2"><code>"existing_email"</code></td>
    </tr>
    <tr>
      <td>Email is Malformed</td>
      <td colspan="2"><code>"malformed_email"</code></td>
    </tr>
    <tr>
      <td>Known bad password</td>
      <td colspan="2"><code>"bad_password"</code></td>
    </tr>
    <tr>
      <td>Password is too short</td>
      <td colspan="2"><code>"short_password"</code></td>
    </tr>
    <tr>
      <th><code>details</code></th>
      <td>Specific error details</td>
      <td><code>object</code></td>
      <td>Optional</td>
    </tr>
    <tr>
      <th><code>.minimum_length</code></th>
      <td><code>short_password</code> minimum password length</td>
      <td><code>Number</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th><code>.required</code></th>
      <td><code>missing_required</code> list of missing field names</td>
      <td><code>string[]</code></td>
      <td>Required</td>
    </tr>
  </tbody>
</table>

<a name="endpoint-register-key"></a>/v1/register/key (Disabled)
------------------

*Temporarily disabled until we need it*

### POST

Create a new user with empty preferences and the ability to login with the
given secret key credentials.

Immediately log the user in and return an authentication token.

<table>
  <tbody>
    <tr>
      <th colspan="4">Headers</th>
    </tr>
    <tr>
      <th><code>Content-Type</code></th>
      <td colspan="2"><code>application/json; charset=utf-8</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4">Request Body</th>
    </tr>
    <tr>
      <th><code>key</code></th>
      <td>The client-derived secret key</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th><code>first_name</code></th>
      <td>The user's first name</td>
      <td><code>String</code></td>
      <td>Optional</td>
    </tr>
    <tr>
      <th><code>last_name</code></th>
      <td>The user's last name</td>
      <td><code>String</code></td>
      <td>Optional</td>
    </tr>
    <tr>
      <th colspan="4"><code>200</code> Response Body</th>
    </tr>
    <tr>
      <th><code>token</code></th>
      <td>The token string to use in the <code>Authorization</code> header</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th><code>user</code></th>
      <td>The user object</td>
      <td><a href="#endpoint-user"><code>User</code></a></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4"><code>400</code> Response Body</th>
    </tr>
    <tr>
      <th rowspan="2"><code>error</code></th>
      <td>Missing required fields</td>
      <td colspan="2"><code>"missing_required"</code></td>
    </tr>
    <tr>
      <td>Key already exists</td>
      <td colspan="2"><code>"existing_key"</code></td>
    </tr>
    <tr>
      <th><code>details</code></th>
      <td>Specific error details</td>
      <td><code>object</code></td>
      <td>Optional</td>
    </tr>
    <tr>
      <th><code>.required</code></th>
      <td><code>missing_required</code> list of missing field names</td>
      <td><code>string[]</code></td>
      <td>Required</td>
    </tr>
  </tbody>
</table>


<a name="section-authentication"></a>Authentication
=================

<a name="endpoint-auth-username"></a>/v1/auth/username
------------------

### POST

Authenticate the given username/password credentials and return a
token that can be used in `Authorization` headers.

<table>
  <tbody>
    <tr>
      <th colspan="4">Headers</th>
    </tr>
    <tr>
      <th><code>Content-Type</code></th>
      <td colspan="2"><code>application/json; charset=utf-8</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4">Request Body</th>
    </tr>
    <tr>
      <th><code>username</code></th>
      <td>The username to authenticate</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th><code>password</code></th>
      <td>The password to authenticate for the username</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4"><code>200</code> Response Body</th>
    </tr>
    <tr>
      <th><code>token</code></th>
      <td>The token string to use in the <code>Authorization</code> header</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th><code>user</code></th>
      <td>The user object</td>
      <td><a href="#endpoint-user"><code>User</code></a></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4"><code>400</code> Response Body</th>
    </tr>
    <tr>
      <th rowspan="3"><code>error</code></th>
      <td>Invalid credentials, including missing fields</td>
      <td colspan="2"><code>"invalid_credentials"</code></td>
    </tr>
    <tr>
      <td>Account is temporarily locked</td>
      <td colspan="2"><code>"locked"</code></td>
    </tr>
    <tr>
      <td>Rate limit exceeded</td>
      <td colspan="2"><code>"rate_limited"</code></td>
    </tr>
    <tr>
      <th><code>details</code></th>
      <td>Specific error details</td>
      <td><code>object</code></td>
      <td>Optional</td>
    </tr>
    <tr>
      <th><code>.timeout</code></th>
      <td><code>locked</code> duration in seconds until unlocked</td>
      <td><code>Number</code></td>
      <td>Required</td>
    </tr>
  </tbody>
</table>

<a name="endpoint-auth-key"></a>/v1/auth/key (Disabled)
------------------

*Temporarily disabled until we need it*

### POST

Authenticate the given secret key credentials and return a
token that can be used in `Authorization` headers.

<table>
  <tbody>
    <tr>
      <th colspan="4">Headers</th>
    </tr>
    <tr>
      <th><code>Content-Type</code></th>
      <td colspan="2"><code>application/json; charset=utf-8</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4">Request Body</th>
    </tr>
    <tr>
      <th><code>key</code></th>
      <td>The secret key to authenticate</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4"><code>200</code> Response Body</th>
    </tr>
    <tr>
      <th><code>token</code></th>
      <td>The token string to use in the <code>Authorization</code> header</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th><code>user</code></th>
      <td>The user object</td>
      <td><a href="#endpoint-user"><code>User</code></a></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4"><code>400</code> Response Body</th>
    </tr>
    <tr>
      <th rowspan="3"><code>error</code></th>
      <td>Invalid credentials, including missing fields</td>
      <td colspan="2"><code>"invalid_credentials"</code></td>
    </tr>
    <tr>
      <td>Rate limit exceeded</td>
      <td colspan="2"><code>"rate_limited"</code></td>
    </tr>
  </tbody>
</table>


<a name="section-user-data"></a>User Data
=================

<a name="endpoint-user"></a>/v1/users/{id}
------------------

### GET

Get the user object for the given `id`

<table>
  <tbody>
    <tr>
      <th colspan="4">Headers</th>
    </tr>
    <tr>
      <th><code>Authorization</code></th>
      <td colspan="2"><code>"Bearer "</code> + Token string obtained from<code>/auth/username</code> or <code>/auth/key</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4"><code>200</code> Response Body</th>
    </tr>
    <tr>
      <th><code>id</code></th>
      <td>The user's unique ID</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th><code>preferences_id</code></th>
      <td>The ID for the user's preferences</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th><code>first_name</code></th>
      <td>The user's first name</td>
      <td><code>String</code></td>
      <td>Optional</td>
    </tr>
    <tr>
      <th><code>last_name</code></th>
      <td>The user's last name</td>
      <td><code>String</code></td>
      <td>Optional</td>
    </tr>
    <tr>
      <th colspan="4"><code>401</code> Response Headers</th>
    </tr>
    <tr>
      <th><code>WWW-Authenticate</code></th>
      <td colspan="2"><code>Bearer</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4"><code>401</code> Response Body</th>
    </tr>
    <tr>
      <td colspan="4">Empty indicates authentication required</td>
    </tr>
    <tr>
      <th colspan="4"><code>403</code> Response Body</th>
    </tr>
    <tr>
      <td colspan="4">Empty indicates unauthorized, regardless of whether the requested record actually exists</td>
    </tr>
  </tbody>
</table>


### PUT 

Save the user object for the given `id`

<table>
  <tbody>
    <tr>
      <th colspan="4">Headers</th>
    </tr>
    <tr>
      <th><code>Content-Type</code></th>
      <td colspan="2"><code>application/json; charset=utf-8</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th><code>Authorization</code></th>
      <td colspan="2"><code>"Bearer "</code> + Token string obtained from<code>/auth/username</code> or <code>/auth/key</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4">Request Body</th>
    </tr>
    <tr>
      <th><code>first_name</code></th>
      <td>The user's first name</td>
      <td><code>String</code></td>
      <td>Optional</td>
    </tr>
    <tr>
      <th><code>last_name</code></th>
      <td>The user's last name</td>
      <td><code>String</code></td>
      <td>Optional</td>
    </tr>
    <tr>
      <th colspan="4"><code>200</code> Response Body</th>
    </tr>
    <tr>
      <td colspan="4">Empty indicates success</td>
    </tr>
    <tr>
      <th colspan="4"><code>401</code> Response Headers</th>
    </tr>
    <tr>
      <th><code>WWW-Authenticate</code></th>
      <td colspan="2"><code>Bearer</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4"><code>401</code> Response Body</th>
    </tr>
    <tr>
      <td colspan="4">Empty indicates authentication required</td>
    </tr>
    <tr>
      <th colspan="4"><code>403</code> Response Body</th>
    </tr>
    <tr>
      <td colspan="4">Empty indicates unauthorized, regardless of whether the requested record actually exists</td>
    </tr>
  </tbody>
</table>

<a name="endpoint-preferences"></a>/v1/users/{uid}/preferences/{id}
------------------

A preference id can be found in the `preferences_id` property of a user object.

### GET

Get the preferences object for the given `id`.

<table>
  <tbody>
    <tr>
      <th colspan="4">Headers</th>
    </tr>
    <tr>
      <th><code>Authorization</code></th>
      <td colspan="2"><code>"Bearer "</code> + Token string obtained from<code>/auth/username</code> or <code>/auth/key</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4"><code>200</code> Response Body</th>
    </tr>
    <tr>
      <th><code>id</code></th>
      <td>The preferences unique ID</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th><code>user_id</code></th>
      <td>The ID for the user that owns the preferences</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th><code>default</code></th>
      <td>The dictionary of solution-specific preferences.  The keys are solution identifiers.  Each solution can have a completely arbitrary object for its preferences.</td>
      <td><code>{String: Object}</code></td>
      <td>Optional</td>
    </tr>
    <tr>
      <th colspan="4"><code>401</code> Response Headers</th>
    </tr>
    <tr>
      <th><code>WWW-Authenticate</code></th>
      <td colspan="2"><code>Bearer</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4"><code>401</code> Response Body</th>
    </tr>
    <tr>
      <td colspan="4">Empty indicates authentication required</td>
    </tr>
    <tr>
      <th colspan="4"><code>403</code> Response Body</th>
    </tr>
    <tr>
      <td colspan="4">Empty indicates unauthorized, regardless of whether the requested record actually exists</td>
    </tr>
    <tr>
      <th colspan="4"><code>404</code> Response Body</th>
    </tr>
    <tr>
      <td colspan="4">Empty indicates invalid preferences id</td>
    </tr>
  </tbody>
</table>


### PUT 

Save the user object for the given `id`

<table>
  <tbody>
    <tr>
      <th colspan="4">Headers</th>
    </tr>
    <tr>
      <th><code>Content-Type</code></th>
      <td colspan="2"><code>application/json; charset=utf-8</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th><code>Authorization</code></th>
      <td colspan="2"><code>"Bearer "</code> + Token string obtained from<code>/auth/username</code> or <code>/auth/key</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4">Request Body</th>
    </tr>
    <tr>
      <th><code>default</code></th>
      <td>The dictionary of solution-specific preferences.  The keys are solution identifiers.  Each solution can have a completely arbitrary object for its preferences.</td>
      <td><code>{String: Object}</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4"><code>200</code> Response Body</th>
    </tr>
    <tr>
      <td colspan="4">Empty indicates success</td>
    </tr>
    <tr>
      <th colspan="4"><code>401</code> Response Headers</th>
    </tr>
    <tr>
      <th><code>WWW-Authenticate</code></th>
      <td colspan="2"><code>Bearer</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4"><code>401</code> Response Body</th>
    </tr>
    <tr>
      <td colspan="4">Empty indicates authentication required</td>
    </tr>
    <tr>
      <th colspan="4"><code>403</code> Response Body</th>
    </tr>
    <tr>
      <td colspan="4">Empty indicates unauthorized, regardless of whether the requested record actually exists</td>
    </tr>
    <tr>
      <th colspan="4"><code>404</code> Response Body</th>
    </tr>
    <tr>
      <td colspan="4">Empty indicates invalid preferences id</td>
    </tr>
  </tbody>
</table>

<a name="endpoint-changepassword"></a>/v1/user/{userId}/password
------------------

### POST

Change the password of an authenticated user. Providing the old password is required
for additional security.

<table>
  <tbody>
    <tr>
      <th colspan="4">Headers</th>
    </tr>
    <tr>
      <th><code>Content-Type</code></th>
      <td colspan="2"><code>application/json; charset=utf-8</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th><code>Authorization</code></th>
      <td colspan="2"><code>"Bearer "</code> + Token string obtained from<code>/auth/username</code> or <code>/auth/key</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4">Request Body</th>
    </tr>
    <tr>
      <th><code>existing_password</code></th>
      <td>The existing password</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th><code>new_password</code></th>
      <td>The new password to set</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th><code>delete_existing_tokens</code></th>
      <td>Delete any existing Auth Tokens</td>
      <td><code>Boolean</code></td>
      <td>Optional</td>
    </tr>
    <tr>
      <th colspan="4"><code>200</code> Response Body</th>
    </tr>
    <tr>
      <th colspan="4"><code>400</code> Response Body</th>
    </tr>
    <tr>
      <th rowspan="4"><code>error</code></th>
      <td>Missing required fields</td>
      <td colspan="2"><code>"missing_required"</code></td>
    </tr>
    <tr>
      <td>Username already exists</td>
      <td colspan="2"><code>"existing_username"</code></td>
    </tr>
    <tr>
      <td>Invalid credentials</td>
      <td colspan="2"><code>"invalid_credentials"</code></td>
    </tr>
    <tr>
      <td>Rate limit exceeded</td>
      <td colspan="2"><code>"rate_limited"</code></td>
    </tr>
  </tbody>
</table>

<a name="section-password-reset"></a>Password Reset
=================

<a name="endpoint-password-reset"></a>/v1/auth/username/password_reset/{oneTimeToken}
------------------

### POST

Reset a password
<table>
  <tbody>
    <tr>
      <th colspan="4">Headers</th>
    </tr>
    <tr>
      <th><code>Content-Type</code></th>
      <td colspan="2"><code>application/json; charset=utf-8</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4">Request Body</th>
    </tr>
    <tr>
      <th><code>new_password</code></th>
      <td>The new password</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th><code>delete_existing_tokens</code></th>
      <td>Whether to terminate all existing auth sessions immediately.</td>
      <td><code>Boolean</code></td>
      <td>Optional (default: false)</td>
    </tr>
    <tr>
      <th colspan="4"><code>200</code> Response Body</th>
    </tr>
    <tr>
      <th colspan="4"><code>400</code> Response Body</th>
    </tr>
    <tr>
      <th rowspan="3"><code>error</code></th>
      <td>Missing required fields</td>
      <td colspan="2"><code>"missing_required"</code></td>
    </tr>
    <tr>
      <td>Invalid One-Time Token</td>
      <td colspan="2"><code>"invalid_token"</code></td>
    </tr>
    <tr>
      <td>User Not found</td>
      <td colspan="2"><code>"invalid_user"</code></td>
    </tr>
    <tr>
      <th><code>details</code></th>
      <td>Specific error details</td>
      <td><code>object</code></td>
      <td>Optional</td>
    </tr>
    <tr>
      <th><code>.required</code></th>
      <td><code>missing_required</code> list of missing field names</td>
      <td><code>string[]</code></td>
      <td>Required</td>
    </tr>
  </tbody>
</table>

<a name="endpoint-password-reset-request"></a>/v1/auth/username/password_reset/request
------------------

### POST

Request a password reset email.

<table>
  <tbody>
    <tr>
      <th colspan="4">Headers</th>
    </tr>
    <tr>
      <th><code>Content-Type</code></th>
      <td colspan="2"><code>application/json; charset=utf-8</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4">Request Body</th>
    </tr>
    <tr>
      <th><code>email</code></th>
      <td>Email to send the password reset email to.</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th><code>g_recaptcha_response</code></th>
      <td>The recaptcha response from the UI</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4"><code>200</code> Response Body</th>
    </tr>
    <tr>
      <th colspan="4"><code>400</code> Response Body</th>
    </tr>
    <tr>
      <th rowspan="2"><code>error</code></th>
      <td>Malformed email address</td>
      <td colspan="2"><code>"bad_email_address"</code></td>
    </tr>
    <tr>
      <td>Bad Recaptcha</td>
      <td colspan="2"><code>"bad_recaptcha"</code></td>
    </tr>
    <tr>
      <th><code>details</code></th>
      <td>Specific error details</td>
      <td><code>object</code></td>
      <td>Optional</td>
    </tr>
    <tr>
      <th><code>.required</code></th>
      <td><code>missing_required</code> list of missing field names</td>
      <td><code>string[]</code></td>
      <td>Required</td>
    </tr>
  </tbody>
</table>
