The Morphic Lite API

Endpoints
=========

* [User Registration](#section-user-registration)
  * [`/v1/register/username`](#endpoint-register-username)
  * [`/v1/register/key`](#endpoint-register-key)
  * [`/v1/invitations/{id}`](#endpoint-invitation)
* [Authentication](#section-authentication)
  * [`/v1/auth/username`](#endpoint-auth-username)
  * [`/v1/auth/key`](#endpoint-auth-key)
* [User Data](#section-user-data)
  * [`/v1/users/{id}`](#endpoint-user)
  * [`/v1/users/{uid}/preferences/{id}`](#endpoint-preferences)
  * [`/v1/users/{id}/password`](#endpoint-password)
  * [`/v1/users/{id}/communities`](#endpoint-user-communities)
  * [`/v1/users/{uid}/communities/{cid}`](#endpoint-user-community)
  * [`/v1/users/{id}/unregister`](#endpoint-user-unregister)
  * [`/v1/users/{id}/resend_verification`](#endpoint-user-resend-verification)
* [Password Reset](#section-password-reset)
  * [`/v1/auth/username/password_reset/{oneTimeToken}`](#endpoint-password-reset)  
  * [`/v1/auth/username/password_reset/request`](#endpoint-password-reset-request)
* [Community](#section-community)
  * [`/v1/communities`](#endpoint-communities)
  * [`/v1/communities/{id}`](#endpoint-community)
  * [`/v1/communities/{id}/members`](#endpoint-community-members)
  * [`/v1/communities/{cid}/members/{id}`](#endpoint-community-member)
  * [`/v1/communities/{id}/invitations`](#endpoint-community-invitations)
  * [`/v1/communities/{cid}/invitations/{id}/accept`](#endpoint-community-invitation-accept)
  * [`/v1/communities/{id}/bars`](#endpoint-community-bars)
  * [`/v1/communities/{cid}/bars/{id}`](#endpoint-community-bar)
  * [`/v1/communities/{id}/skype/meetings`](#endpoint-community-skype-meetings)
  * [`/v1/communities/{id}/billing`](#endpoint-community-billing)
  * [`/v1/communities/{id}/billing/card`](#endpoint-community-billing-card)
  * [`/v1/communities/{id}/billing/cancel`](#endpoint-community-billing-cancel)
* [Plans](#section-plans)
  * [`v1/plans/community`](#endpoint-plans-community)

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
      <td><code>Content-Type</code></td>
      <td colspan="2"><code>application/json; charset=utf-8</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4">Request Body</th>
    </tr>
    <tr>
      <td><code>username</code></td>
      <td>The user-chosen username</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td><code>password</code></td>
      <td>The user-chosen password</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td><code>email</code></td>
      <td>The user's email</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td><code>first_name</code></td>
      <td>The user's first name</td>
      <td><code>String</code></td>
      <td>Optional</td>
    </tr>
    <tr>
      <td><code>last_name</code></td>
      <td>The user's last name</td>
      <td><code>String</code></td>
      <td>Optional</td>
    </tr>
    <tr>
      <th colspan="4"><code>200</code> Response Body</th>
    </tr>
    <tr>
      <td><code>token</code></td>
      <td>The token string to use in the <code>Authorization</code> header</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td><code>user</code></td>
      <td>The user object</td>
      <td><a href="#endpoint-user"><code>User</code></a></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4"><code>400</code> Response Body</th>
    </tr>
    <tr>
      <td rowspan="6"><code>error</code></td>
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
      <td><code>details</code></td>
      <td>Specific error details</td>
      <td><code>object</code></td>
      <td>Optional</td>
    </tr>
    <tr>
      <td>&nbsp;&nbsp;&nbsp;&nbsp;<code>.minimum_length</code></td>
      <td><code>short_password</code> minimum password length</td>
      <td><code>Number</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td>&nbsp;&nbsp;&nbsp;&nbsp;<code>.required</code></td>
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
      <td><code>Content-Type</code></td>
      <td colspan="2"><code>application/json; charset=utf-8</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4">Request Body</th>
    </tr>
    <tr>
      <td><code>key</code></td>
      <td>The client-derived secret key</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td><code>first_name</code></td>
      <td>The user's first name</td>
      <td><code>String</code></td>
      <td>Optional</td>
    </tr>
    <tr>
      <td><code>last_name</code></td>
      <td>The user's last name</td>
      <td><code>String</code></td>
      <td>Optional</td>
    </tr>
    <tr>
      <th colspan="4"><code>200</code> Response Body</th>
    </tr>
    <tr>
      <td><code>token</code></td>
      <td>The token string to use in the <code>Authorization</code> header</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td><code>user</code></td>
      <td>The user object</td>
      <td><a href="#endpoint-user"><code>User</code></a></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4"><code>400</code> Response Body</th>
    </tr>
    <tr>
      <td rowspan="2"><code>error</code></td>
      <td>Missing required fields</td>
      <td colspan="2"><code>"missing_required"</code></td>
    </tr>
    <tr>
      <td>Key already exists</td>
      <td colspan="2"><code>"existing_key"</code></td>
    </tr>
    <tr>
      <td><code>details</code></td>
      <td>Specific error details</td>
      <td><code>object</code></td>
      <td>Optional</td>
    </tr>
    <tr>
      <td>&nbsp;&nbsp;&nbsp;&nbsp;<code>.required</code></td>
      <td><code>missing_required</code> list of missing field names</td>
      <td><code>string[]</code></td>
      <td>Required</td>
    </tr>
  </tbody>
</table>


<a name="endpoint-invitation"></a>/v1/invitations/{id}
------------------

### GET

Get the details of an invitation to show a custom user registration or login screen.

The personal information returned can be used to pre-fill a user registration form.

After the user is registered or authenticated, complete the invitation process by POSTing to
the `/v1/community/{cid}/invitations/{id}/accept` endpoint.

<table>
  <tbody>
    <tr>
      <th colspan="4">Headers</th>
    </tr>
    <tr>
      <th colspan="4"><code>200</code> Response Body</th>
    </tr>
    <tr>
      <td><code>community</code></td>
      <td>The community the user was invited to</td>
      <td><code>object</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td>&nbsp;&nbsp;&nbsp;&nbsp;<code>.id</code></td>
      <td>The community's id</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td>&nbsp;&nbsp;&nbsp;&nbsp;<code>.name</code></td>
      <td>The community's name</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td><code>email</code></td>
      <td>The invitee's email</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td><code>first_name</code></td>
      <td>The invitee's first name</td>
      <td><code>String</code></td>
      <td>Optional</td>
    </tr>
    <tr>
      <td><code>last_name</code></td>
      <td>The invitee's last name</td>
      <td><code>String</code></td>
      <td>Optional</td>
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
      <td><code>Content-Type</code></td>
      <td colspan="2"><code>application/json; charset=utf-8</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4">Request Body</th>
    </tr>
    <tr>
      <td><code>username</code></td>
      <td>The username to authenticate</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td><code>password</code></td>
      <td>The password to authenticate for the username</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4"><code>200</code> Response Body</th>
    </tr>
    <tr>
      <td><code>token</code></td>
      <td>The token string to use in the <code>Authorization</code> header</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td><code>user</code></td>
      <td>The user object</td>
      <td><a href="#endpoint-user"><code>User</code></a></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4"><code>400</code> Response Body</th>
    </tr>
    <tr>
      <td rowspan="3"><code>error</code></td>
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
      <td><code>details</code></td>
      <td>Specific error details</td>
      <td><code>object</code></td>
      <td>Optional</td>
    </tr>
    <tr>
      <td>&nbsp;&nbsp;&nbsp;&nbsp;<code>.timeout</code></td>
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
      <td><code>Content-Type</code></td>
      <td colspan="2"><code>application/json; charset=utf-8</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4">Request Body</th>
    </tr>
    <tr>
      <td><code>key</code></td>
      <td>The secret key to authenticate</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4"><code>200</code> Response Body</th>
    </tr>
    <tr>
      <td><code>token</code></td>
      <td>The token string to use in the <code>Authorization</code> header</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td><code>user</code></td>
      <td>The user object</td>
      <td><a href="#endpoint-user"><code>User</code></a></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4"><code>400</code> Response Body</th>
    </tr>
    <tr>
      <td rowspan="3"><code>error</code></td>
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
      <td><code>Authorization</code></td>
      <td colspan="2"><code>"Bearer "</code> + Token string obtained from<code>/auth/username</code> or <code>/auth/key</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4"><code>200</code> Response Body</th>
    </tr>
    <tr>
      <td><code>id</code></td>
      <td>The user's unique ID</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td><code>preferences_id</code></td>
      <td>The ID for the user's preferences</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td><code>first_name</code></td>
      <td>The user's first name</td>
      <td><code>String</code></td>
      <td>Optional</td>
    </tr>
    <tr>
      <td><code>last_name</code></td>
      <td>The user's last name</td>
      <td><code>String</code></td>
      <td>Optional</td>
    </tr>
    <tr>
      <th colspan="4"><code>401</code> Response Headers</th>
    </tr>
    <tr>
      <td><code>WWW-Authenticate</code></td>
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
      <td><code>Content-Type</code></td>
      <td colspan="2"><code>application/json; charset=utf-8</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td><code>Authorization</code></td>
      <td colspan="2"><code>"Bearer "</code> + Token string obtained from<code>/auth/username</code> or <code>/auth/key</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4">Request Body</th>
    </tr>
    <tr>
      <td><code>first_name</code></td>
      <td>The user's first name</td>
      <td><code>String</code></td>
      <td>Optional</td>
    </tr>
    <tr>
      <td><code>last_name</code></td>
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
      <td><code>WWW-Authenticate</code></td>
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
      <td><code>Authorization</code></td>
      <td colspan="2"><code>"Bearer "</code> + Token string obtained from<code>/auth/username</code> or <code>/auth/key</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4"><code>200</code> Response Body</th>
    </tr>
    <tr>
      <td><code>id</code></td>
      <td>The preferences unique ID</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td><code>user_id</code></td>
      <td>The ID for the user that owns the preferences</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td><code>default</code></td>
      <td>The dictionary of solution-specific preferences.  The keys are solution identifiers.  Each solution can have a completely arbitrary object for its preferences.</td>
      <td><code>{String: Object}</code></td>
      <td>Optional</td>
    </tr>
    <tr>
      <th colspan="4"><code>401</code> Response Headers</th>
    </tr>
    <tr>
      <td><code>WWW-Authenticate</code></td>
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
      <td><code>Content-Type</code></td>
      <td colspan="2"><code>application/json; charset=utf-8</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td><code>Authorization</code></td>
      <td colspan="2"><code>"Bearer "</code> + Token string obtained from<code>/auth/username</code> or <code>/auth/key</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4">Request Body</th>
    </tr>
    <tr>
      <td><code>default</code></td>
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
      <td><code>WWW-Authenticate</code></td>
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

<a name="endpoint-password"></a>/v1/users/{userId}/password

### POST

Change the password of an authenticated user. Providing the old password is required
for additional security.

<table>
  <tbody>
    <tr>
      <th colspan="4">Headers</th>
    </tr>
    <tr>
      <td><code>Content-Type</code></td>
      <td colspan="2"><code>application/json; charset=utf-8</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td><code>Authorization</code></td>
      <td colspan="2"><code>"Bearer "</code> + Token string obtained from<code>/auth/username</code> or <code>/auth/key</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4">Request Body</th>
    </tr>
    <tr>
      <td><code>existing_password</code></td>
      <td>The existing password</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td><code>new_password</code></td>
      <td>The new password to set</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td><code>delete_existing_tokens</code></td>
      <td>Delete any existing Auth Tokens</td>
      <td><code>Boolean</code></td>
      <td>Optional</td>
    </tr>
    <tr>
      <th colspan="4"><code>200</code> Response Body</th>
    </tr>
    <tr>
      <td colspan="4">Empty indicates success</td>
    </tr>
    <tr>
      <th colspan="4"><code>400</code> Response Body</th>
    </tr>
    <tr>
      <td rowspan="4"><code>error</code></td>
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


<a name="endpoint-user-communities"></a>/v1/users/{id}/communities
------------------

### GET

Get a list of communities the user belongs to.

<table>
  <tbody>
    <tr>
      <th colspan="4">Headers</th>
    </tr>
    <tr>
      <td><code>Authorization</code></td>
      <td colspan="2"><code>"Bearer "</code> + Token string obtained from<code>/auth/username</code> or <code>/auth/key</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4"><code>200</code> Response Body</th>
    </tr>
    <tr>
      <td><code>communities</code></td>
      <td>The communities the user belongs to</td>
      <td><code>Array</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td>&nbsp;&nbsp;&nbsp;&nbsp;<code>[i].id</code></td>
      <td>The community unique identifier</td>
      <td><code>string</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td>&nbsp;&nbsp;&nbsp;&nbsp;<code>[i].name</code></td>
      <td>The community display name</td>
      <td><code>string</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td>&nbsp;&nbsp;&nbsp;&nbsp;<code>[i].role</code></td>
      <td>The role the user has in the community</td>
      <td><code>Role</code></td>
      <td>Required</td>
    </tr>
  </tbody>
</table>

#### Role: String
* `manager`
* `member`


<a name="endpoint-user-community"></a>/v1/users/{uid}/communities/{id}
------------------

### GET

The details of a community the user belongs to

<table>
  <tbody>
    <tr>
      <th colspan="4">Headers</th>
    </tr>
    <tr>
      <td><code>Authorization</code></td>
      <td colspan="2"><code>"Bearer "</code> + Token string obtained from<code>/auth/username</code> or <code>/auth/key</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4"><code>200</code> Response Body</th>
    </tr>
    <tr>
      <td><code>id</code></td>
      <td>The community unique identifier</td>
      <td><code>string</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td><code>name</code></td>
      <td>The community display name</td>
      <td><code>string</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td><code>bar</code></td>
      <td>The bar to show for this user</td>
      <td><code>object</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td>&nbsp;&nbsp;&nbsp;&nbsp;<code>.id</code></td>
      <td>The bar's id</td>
      <td><code>string</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td>&nbsp;&nbsp;&nbsp;&nbsp;<code>.name</code></td>
      <td>The bar's name</td>
      <td><code>string</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td>&nbsp;&nbsp;&nbsp;&nbsp;<code>.items</code></td>
      <td>The items shown on the bar</td>
      <td><code>Array</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<code>[i].kind</code></td>
      <td>The bar item's type</td>
      <td><code>BarItemKind</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<code>[i].is_primary</code></td>
      <td>Whether the item should be displayed on the primary bar</td>
      <td><code>Boolean</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<code>[i].configuration</code></td>
      <td>The bar item's configuration, depending on its <code>kind</code></td>
      <td><code>object</code></td>
      <td>Optional</td>
    </tr>
    <tr>
      <th colspan="4"><code>400</code> Response Body</th>
    </tr>
    <tr>
      <td><code>error</code></td>
      <td>The community is locked, possibly because of a payment problem</td>
      <td colspan="2"><code>"community_locked"</code></td>
    </tr>
  </tbody>
</table>

#### BarItemKind: String
* `link`
* `application`
* `action`

<a name="endpoint-user-unregister"></a>/v1/users/{id}/unregister
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
      <th><code>Authorization</code></th>
      <td colspan="2"><code>"Bearer "</code> + Token string obtained from<code>/auth/username</code> or <code>/auth/key</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4"><code>200</code> Response Body</th>
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

<a name="endpoint-user-resend-verification"></a>/v1/users/{id}/resend_verification
------------------

### POST

Re-send the user's verification email.

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
      <th colspan="4"><code>200</code> Response Body</th>
    </tr>
    <tr>
      <td colspan="4">Empty indicates success</td>
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
      <td><code>Content-Type</code></td>
      <td colspan="2"><code>application/json; charset=utf-8</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4">Request Body</th>
    </tr>
    <tr>
      <td><code>new_password</code></td>
      <td>The new password</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td><code>delete_existing_tokens</code></td>
      <td>Whether to terminate all existing auth sessions immediately.</td>
      <td><code>Boolean</code></td>
      <td>Optional (default: false)</td>
    </tr>
    <tr>
      <th colspan="4"><code>200</code> Response Body</th>
    </tr>
    <tr>
      <td colspan="4">Empty indicates success</td>
    </tr>
    <tr>
      <th colspan="4"><code>400</code> Response Body</th>
    </tr>
    <tr>
      <td rowspan="3"><code>error</code></td>
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
      <td><code>details</code></td>
      <td>Specific error details</td>
      <td><code>object</code></td>
      <td>Optional</td>
    </tr>
    <tr>
      <td>&nbsp;&nbsp;&nbsp;&nbsp;<code>.required</code></td>
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
      <td><code>Content-Type</code></td>
      <td colspan="2"><code>application/json; charset=utf-8</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4">Request Body</th>
    </tr>
    <tr>
      <td><code>email</code></td>
      <td>Email to send the password reset email to.</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td><code>g_recaptcha_response</code></td>
      <td>The recaptcha response from the UI</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4"><code>200</code> Response Body</th>
    </tr>
    <tr>
      <td colspan="4">Empty indicates success</td>
    </tr>
    <tr>
      <th colspan="4"><code>400</code> Response Body</th>
    </tr>
    <tr>
      <td rowspan="2"><code>error</code></td>
      <td>Malformed email address</td>
      <td colspan="2"><code>"bad_email_address"</code></td>
    </tr>
    <tr>
      <td>Bad Recaptcha</td>
      <td colspan="2"><code>"bad_recaptcha"</code></td>
    </tr>
    <tr>
      <td><code>details</code></td>
      <td>Specific error details</td>
      <td><code>object</code></td>
      <td>Optional</td>
    </tr>
    <tr>
      <td>&nbsp;&nbsp;&nbsp;&nbsp;<code>.required</code></td>
      <td><code>missing_required</code> list of missing field names</td>
      <td><code>string[]</code></td>
      <td>Required</td>
    </tr>
  </tbody>
</table>


<a name="section-community"></a>Community
=================

<a name="endpoint-communities"></a>/v1/communities
------------------

### POST

Create a new community

<table>
  <tbody>
    <tr>
      <th colspan="4">Headers</th>
    </tr>
    <tr>
      <td><code>Content-Type</code></td>
      <td colspan="2"><code>application/json; charset=utf-8</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td><code>Authorization</code></td>
      <td colspan="2"><code>"Bearer "</code> + Token string obtained from<code>/auth/username</code> or <code>/auth/key</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4">Request Body</th>
    </tr>
    <tr>
      <td><code>name</code></td>
      <td>The new community name</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4"><code>200</code> Response Body</th>
    </tr>
    <tr>
      <td><code>community</code></td>
      <td>The newly created community</td>
      <td><code><a href="#endpoint-community">Community</a></code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4"><code>400</code> Response Body</th>
    </tr>
    <tr>
      <td><code>error</code></td>
      <td>Missing required fields</td>
      <td colspan="2"><code>"missing_required"</code></td>
    </tr>
    <tr>
      <td><code>details</code></td>
      <td>Specific error details</td>
      <td><code>object</code></td>
      <td>Optional</td>
    </tr>
    <tr>
      <td>&nbsp;&nbsp;&nbsp;&nbsp;<code>.required</code></td>
      <td><code>missing_required</code> list of missing field names</td>
      <td><code>string[]</code></td>
      <td>Required</td>
    </tr>
  </tbody>
</table>

<a name="endpoint-community"></a>/v1/communities/{id}
------------------

### GET

Get a community's details

<table>
  <tbody>
    <tr>
      <th colspan="4">Headers</th>
    </tr>
    <tr>
      <td><code>Authorization</code></td>
      <td colspan="2"><code>"Bearer "</code> + Token string obtained from<code>/auth/username</code> or <code>/auth/key</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4"><code>200</code> Response Body</th>
    </tr>
    <tr>
      <td><code>id</code></td>
      <td>The community id</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td><code>name</code></td>
      <td>The new community name</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td><code>default_bar_id</code></td>
      <td>The default bar for this community</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td><code>member_count</code></td>
      <td>The number of members in the community that count towards the plan maximum</td>
      <td><code>int</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td><code>member_limit</code></td>
      <td>The maximum number of members allowed according to the plan</td>
      <td><code>int</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td><code>is_locked</code></td>
      <td>Indicates the community is locked because of payment issues</td>
      <td><code>Boolean</code></td>
      <td>Required</td>
    </tr>
  </tbody>
</table>

### PUT

Update a community

<table>
  <tbody>
    <tr>
      <th colspan="4">Headers</th>
    </tr>
    <tr>
      <td><code>Content-Type</code></td>
      <td colspan="2"><code>application/json; charset=utf-8</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td><code>Authorization</code></td>
      <td colspan="2"><code>"Bearer "</code> + Token string obtained from<code>/auth/username</code> or <code>/auth/key</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4">Request Body</th>
    </tr>
    <tr>
      <td><code>name</code></td>
      <td>The new community name</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td><code>default_bar_id</code></td>
      <td>The new default bar id for this community</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4"><code>200</code> Response Body</th>
    </tr>
    <tr>
      <td colspan="4">Empty indicates success</td>
    </tr>
    <tr>
      <th colspan="4"><code>400</code> Response Body</th>
    </tr>
    <tr>
      <td rowspan="2"><code>error</code></td>
      <td>Missing required fields</td>
      <td colspan="2"><code>"missing_required"</code></td>
    </tr>
    <tr>
      <td>The Bar ID given could not be found</td>
      <td colspan="2"><code>"bad_bar_id"</code></td>
    </tr>
    <tr>
      <td><code>details</code></td>
      <td>Specific error details</td>
      <td><code>object</code></td>
      <td>Optional</td>
    </tr>
    <tr>
      <td>&nbsp;&nbsp;&nbsp;&nbsp;<code>.required</code></td>
      <td><code>missing_required</code> list of missing field names</td>
      <td><code>string[]</code></td>
      <td>Required</td>
    </tr>
  </tbody>
</table>

### DELETE

Delete a community

<table>
  <tbody>
    <tr>
      <th colspan="4">Headers</th>
    </tr>
    <tr>
      <td><code>Authorization</code></td>
      <td colspan="2"><code>"Bearer "</code> + Token string obtained from<code>/auth/username</code> or <code>/auth/key</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4"><code>200</code> Response Body</th>
    </tr>
    <tr>
      <td colspan="4">Empty indicates success</td>
    </tr>
  </tbody>
</table>


<a name="endpoint-community-members"></a>/v1/communities/{id}/members
------------------

### GET

Get a list of community members

<table>
  <tbody>
    <tr>
      <th colspan="4">Headers</th>
    </tr>
    <tr>
      <td><code>Authorization</code></td>
      <td colspan="2"><code>"Bearer "</code> + Token string obtained from<code>/auth/username</code> or <code>/auth/key</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4"><code>200</code> Response Body</th>
    </tr>
    <tr>
      <td><code>members</code></td>
      <td>The members for this page of results</td>
      <td>Array</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td>&nbsp;&nbsp;&nbsp;&nbsp;<code>[i].id</code></td>
      <td>The member's unique id</td>
      <td>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td>&nbsp;&nbsp;&nbsp;&nbsp;<code>[i].first_name</code></td>
      <td>The member's first name</td>
      <td>String</code></td>
      <td>Optional</td>
    </tr>
    <tr>
      <td>&nbsp;&nbsp;&nbsp;&nbsp;<code>[i].last_name</code></td>
      <td>The member's last name</td>
      <td>String</code></td>
      <td>Optional</td>
    </tr>
    <tr>
      <td>&nbsp;&nbsp;&nbsp;&nbsp;<code>[i].role</code></td>
      <td>The member's role</td>
      <td>Role</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td>&nbsp;&nbsp;&nbsp;&nbsp;<code>[i].state</code></td>
      <td>The member's state</td>
      <td>State</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td>&nbsp;&nbsp;&nbsp;&nbsp;<code>[i].bar_id</code></td>
      <td>The member's bar_id (null means use community default_bar_id)</td>
      <td>String</code></td>
      <td>Optional</td>
    </tr>
  </tbody>
</table>


### POST

Create a new community member

<table>
  <tbody>
    <tr>
      <th colspan="4">Headers</th>
    </tr>
    <tr>
      <td><code>Content-Type</code></td>
      <td colspan="2"><code>application/json; charset=utf-8</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td><code>Authorization</code></td>
      <td colspan="2"><code>"Bearer "</code> + Token string obtained from<code>/auth/username</code> or <code>/auth/key</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4">Request Body</th>
    </tr>
    <tr>
      <td><code>first_name</code></td>
      <td>The member's first name</td>
      <td><code>String</code></td>
      <td>Optional</td>
    </tr>
    <tr>
      <td><code>last_name</code></td>
      <td>The member's last name</td>
      <td><code>String</code></td>
      <td>Optional</td>
    </tr>
    <tr>
      <th colspan="4"><code>200</code> Response Body</th>
    </tr>
    <tr>
      <td><code>member</code></td>
      <td>The newly created member</td>
      <td><code><a href="#endpoint-community-member">Member</a></code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4"><code>400</code> Response Body</th>
    </tr>
    <tr>
      <td rowspan="2"><code>error</code></td>
      <td>Missing required fields</td>
      <td colspan="2"><code>"missing_required"</code></td>
    </tr>
    <tr>
      <td>Member Limit reached for current plan</td>
      <td colspan="2"><code>"limit_reached"</code></td>
    </tr>
    <tr>
      <td><code>details</code></td>
      <td>Specific error details</td>
      <td><code>object</code></td>
      <td>Optional</td>
    </tr>
    <tr>
      <td>&nbsp;&nbsp;&nbsp;&nbsp;<code>.required</code></td>
      <td><code>missing_required</code> list of missing field names</td>
      <td><code>string[]</code></td>
      <td>Required</td>
    </tr>
  </tbody>
</table>

<a name="endpoint-community-member"></a>/v1/communities/{cid}/members/{id}
------------------

### GET

Get a community member's details

<table>
  <tbody>
    <tr>
      <th colspan="4">Headers</th>
    </tr>
    <tr>
      <td><code>Authorization</code></td>
      <td colspan="2"><code>"Bearer "</code> + Token string obtained from<code>/auth/username</code> or <code>/auth/key</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4"><code>200</code> Response Body</th>
    </tr>
    <tr>
      <td><code>id</code></td>
      <td>The member's unique id</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td><code>first_name</code></td>
      <td>The member's first name</td>
      <td><code>String</code></td>
      <td>Optional</td>
    </tr>
    <tr>
      <td><code>last_name</code></td>
      <td>The member's last name</td>
      <td><code>String</code></td>
      <td>Optional</td>
    </tr>
    <tr>
      <td><code>role</code></td>
      <td>The member's role</td>
      <td><code>Role</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td><code>state</code></td>
      <td>The member's state</td>
      <td><code>State</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td><code>bar_id</code></td>
      <td>The member's bar, if not the community's default</td>
      <td><code>String</code></td>
      <td>Optional</td>
    </tr>
  </tbody>
</table>

#### Role: String
* `manager`
* `member`

#### State: String
* `uninvited` - Added, but not yet invited; still configuring
* `invited` - Invitation sent, but not accepted
* `active` - Invitation accepted

### PUT

Update a community member's details

<table>
  <tbody>
    <tr>
      <th colspan="4">Headers</th>
    </tr>
    <tr>
      <td><code>Content-Type</code></td>
      <td colspan="2"><code>application/json; charset=utf-8</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td><code>Authorization</code></td>
      <td colspan="2"><code>"Bearer "</code> + Token string obtained from<code>/auth/username</code> or <code>/auth/key</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4">Request Body</th>
    </tr>
    <tr>
      <td><code>first_name</code></td>
      <td>The member's first name</td>
      <td><code>String</code></td>
      <td>Optional</td>
    </tr>
    <tr>
      <td><code>last_name</code></td>
      <td>The member's last name</td>
      <td><code>String</code></td>
      <td>Optional</td>
    </tr>
    <tr>
      <td><code>bar_id</code></td>
      <td>The member's bar, if not the community's default</td>
      <td><code>String</code></td>
      <td>Optional</td>
    </tr>
    <tr>
      <td><code>role</code></td>
      <td>The member's role</td>
      <td><code>Role</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4"><code>200</code> Response Body</th>
    </tr>
    <tr>
      <td colspan="4">Empty indicates success</td>
    </tr>
    <tr>
      <th colspan="4"><code>400</code> Response Body</th>
    </tr>
    <tr>
      <td rowspan="3"><code>error</code></td>
      <td>Missing required fields</td>
      <td colspan="2"><code>"missing_required"</code></td>
    </tr>
    <tr>
      <td>Bar could not be found</td>
      <td colspan="2"><code>"bad_bar_id"</code></td>
    </tr>
    <tr>
      <td>Cannot demote self from manager role</td>
      <td colspan="2"><code>"cannot_demote_self"</code></td>
    </tr>
    <tr>
      <td><code>details</code></td>
      <td>Specific error details</td>
      <td><code>object</code></td>
      <td>Optional</td>
    </tr>
    <tr>
      <td>&nbsp;&nbsp;&nbsp;&nbsp;<code>.required</code></td>
      <td><code>missing_required</code> list of missing field names</td>
      <td><code>string[]</code></td>
      <td>Required</td>
    </tr>
  </tbody>
</table>

### DELETE

Delete a member from a community

<table>
  <tbody>
    <tr>
      <th colspan="4">Headers</th>
    </tr>
    <tr>
      <td><code>Authorization</code></td>
      <td colspan="2"><code>"Bearer "</code> + Token string obtained from<code>/auth/username</code> or <code>/auth/key</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4"><code>200</code> Response Body</th>
    </tr>
    <tr>
      <td colspan="4">Empty indicates success</td>
    </tr>
    <tr>
      <th colspan="4"><code>400</code> Response Body</th>
    </tr>
    <tr>
      <td ><code>error</code></td>
      <td>Cannot delete self</td>
      <td colspan="2"><code>"cannot_delete_self"</code></td>
    </tr>
  </tbody>
</table>


<a name="endpoint-community-invitations"></a>/v1/communities/{id}/invitations
------------------


### POST

Send a new invitation for a community member

<table>
  <tbody>
    <tr>
      <th colspan="4">Headers</th>
    </tr>
    <tr>
      <td><code>Content-Type</code></td>
      <td colspan="2"><code>application/json; charset=utf-8</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td><code>Authorization</code></td>
      <td colspan="2"><code>"Bearer "</code> + Token string obtained from<code>/auth/username</code> or <code>/auth/key</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4">Request Body</th>
    </tr>
    <tr>
      <td><code>member_id</code></td>
      <td>The member's unique identifier</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td><code>email</code></td>
      <td>The member's email address, if not already added</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td><code>message</code></td>
      <td>A custom message that will be included in the email</td>
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
      <th colspan="4"><code>400</code> Response Body</th>
    </tr>
    <tr>
      <td rowspan="5"><code>error</code></td>
      <td>Missing required fields</td>
      <td colspan="2"><code>"missing_required"</code></td>
    </tr>
    <tr>
      <td>Invalid Email Address</td>
      <td colspan="2"><code>"malformed_email"</code></td>
    </tr>
    <tr>
      <td>Member not found</td>
      <td colspan="2"><code>"member_not_found"</code></td>
    </tr>
    <tr>
      <td>Active Member</td>
      <td colspan="2"><code>"member_active"</code></td>
    </tr>
    <tr>
      <td>Manager Email Verification Required</td>
      <td colspan="2"><code>"email_verification_required"</code></td>
    </tr>
    <tr>
      <td><code>details</code></td>
      <td>Specific error details</td>
      <td><code>object</code></td>
      <td>Optional</td>
    </tr>
    <tr>
      <td>&nbsp;&nbsp;&nbsp;&nbsp;<code>.required</code></td>
      <td><code>missing_required</code> list of missing field names</td>
      <td><code>string[]</code></td>
      <td>Required</td>
    </tr>
  </tbody>
</table>


<a name="endpoint-community-invitation-accept"></a>/v1/communities/{cid}/invitations/{id}/accept
------------------

### POST

Accept an invitation

<table>
  <tbody>
    <tr>
      <th colspan="4">Headers</th>
    </tr>
    <tr>
      <td><code>Content-Type</code></td>
      <td colspan="2"><code>application/json; charset=utf-8</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4"><code>200</code> Response Body</th>
    </tr>
    <tr>
      <td colspan="4">Empty indicates success</td>
    </tr>
  </tbody>
</table>

<a name="endpoint-community-bars"></a>/v1/communities/{id}/bars
------------------

### GET

Get the list of bar configurations for the community

<table>
  <tbody>
    <tr>
      <th colspan="4">Headers</th>
    </tr>
    <tr>
      <td><code>Authorization</code></td>
      <td colspan="2"><code>"Bearer "</code> + Token string obtained from<code>/auth/username</code> or <code>/auth/key</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4"><code>200</code> Response Body</th>
    </tr>
    <tr>
      <td><code>bars</code></td>
      <td>The list of bars</td>
      <td><code>Array</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td>&nbsp;&nbsp;&nbsp;&nbsp;<code>[i].id</code></td>
      <td>The bar's unique id</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td>&nbsp;&nbsp;&nbsp;&nbsp;<code>[i].name</code></td>
      <td>The bar's display name</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td>&nbsp;&nbsp;&nbsp;&nbsp;<code>[i].is_shared</code></td>
      <td>Whether the bar is shown in the list of preconfigured bars that can be shared across multiple users</td>
      <td><code>Boolean</code></td>
      <td>Required</td>
    </tr>
  </tbody>
</table>

### POST

Create a new bar configuration for the community

<table>
  <tbody>
    <tr>
      <th colspan="4">Headers</th>
    </tr>
    <tr>
      <td><code>Content-Type</code></td>
      <td colspan="2"><code>application/json; charset=utf-8</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td><code>Authorization</code></td>
      <td colspan="2"><code>"Bearer "</code> + Token string obtained from<code>/auth/username</code> or <code>/auth/key</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4">Request Body</th>
    </tr>
    <tr>
      <td><code>name</code></td>
      <td>The bar's name</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td><code>is_shared</code></td>
      <td>Whether the bar is shown in the list of preconfigured bars that can be shared across multiple users</td>
      <td><code>Boolean</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td><code>items</code></td>
      <td>The bar's items</td>
      <td><code>Array</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td>&nbsp;&nbsp;&nbsp;&nbsp;<code>[i].kind</code></td>
      <td>The bar item's type</td>
      <td><code>BarItemKind</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td>&nbsp;&nbsp;&nbsp;&nbsp;<code>[i].is_primary</code></td>
      <td>If the item should be shown on the primary bar</td>
      <td><code>Boolean</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td>&nbsp;&nbsp;&nbsp;&nbsp;<code>[i].configuration</code></td>
      <td>The bar item's configuration, depending on its <code>kind</code></td>
      <td><code>object</code></td>
      <td>Optional</td>
    </tr>
    <tr>
      <th colspan="4"><code>200</code> Response Body</th>
    </tr>
    <tr>
      <td><code>bar</code></td>
      <td>The newly created bar</td>
      <td><code><a href="#endpoint-community-bar">Bar</a></code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4"><code>400</code> Response Body</th>
    </tr>
    <tr>
      <td><code>error</code></td>
      <td>Missing required fields</td>
      <td colspan="2"><code>"missing_required"</code></td>
    </tr>
    <tr>
      <td><code>details</code></td>
      <td>Specific error details</td>
      <td><code>object</code></td>
      <td>Optional</td>
    </tr>
    <tr>
      <td>&nbsp;&nbsp;&nbsp;&nbsp;<code>.required</code></td>
      <td><code>missing_required</code> list of missing field names</td>
      <td><code>string[]</code></td>
      <td>Required</td>
    </tr>
  </tbody>
</table>


<a name="endpoint-community-bar"></a>/v1/communities/{cid}/bars/{id}
------------------

### GET

Get the details of a particular bar

<table>
  <tbody>
    <tr>
      <th colspan="4">Headers</th>
    </tr>
    <tr>
      <td><code>Authorization</code></td>
      <td colspan="2"><code>"Bearer "</code> + Token string obtained from<code>/auth/username</code> or <code>/auth/key</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4"><code>200</code> Response Body</th>
    </tr>
    <tr>
      <td><code>id</code></td>
      <td>The bar's unique id</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td><code>name</code></td>
      <td>The bar's display name</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td><code>is_shared</code></td>
      <td>Whether the bar is shown in the main bar list</td>
      <td><code>Boolean</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td><code>items</code></td>
      <td>The bar's items</td>
      <td><code>Array</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td>&nbsp;&nbsp;&nbsp;&nbsp;<code>[i].kind</code></td>
      <td>The bar item's type</td>
      <td><code>BarItemKind</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td>&nbsp;&nbsp;&nbsp;&nbsp;<code>[i].is_primary</code></td>
      <td>If the item is shown on the primary bar</td>
      <td><code>Boolean</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td>&nbsp;&nbsp;&nbsp;&nbsp;<code>[i].configuration</code></td>
      <td>The bar item's configuration, depending on its <code>kind</code></td>
      <td><code>object</code></td>
      <td>Optional</td>
    </tr>
  </tbody>
</table>

#### BarItemKind: String
* `link` (uses `LinkConfiguration` for `configuration`)
* `application` (uses `ApplicationConfiguration` for `configuration`)
* `action`  (uses `ActionConfiguration` for `configuration`)

#### Bar Item configuration types

````
ButtonConfiguration: object{
  # The user-visible text shown on the button 
  label: string

  # RRGGBB hex string with leading "#"
  color: string?

  # The user-visible image shown on the button
  # Absolute URL for custom uploaded images (e.g., "http://bucket.s3/path/to/image")
  # Filename (relative url) for client-bundled icons (e.g., "video")
  image_url: string?
}
````

````
LinkConfiguration: ButtonConfiguration{
  # The url that should open as the result of clicking the button
  url: string

  # The subkind of link
  # Meaningful for showing the appropriate editor UI in the web client
  # Not used by the desktop client
  # Values determined by the web client, but might be strings like "skype" or "zoom"
  subkind: String?
}
````

````
ApplicationConfiguration: ButtonConfiguration{
  # Indicates the button should open the OS-default application of the given type
  # Possible values include:
  # - email
  # - calendar
  # - browser
  default: string?

  # The name or full path of the windows exe to open
  # (names are looked up in the registry HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths)
  exe: string?
}
````

````
ActionConfiguration: object{
  # The action identifier
  identifier: string

  # RRGGBB hex string with leading "#"
  color: string?
}
````

### PUT

Update a bar's configuration

<table>
  <tbody>
    <tr>
      <th colspan="4">Headers</th>
    </tr>
    <tr>
      <td><code>Content-Type</code></td>
      <td colspan="2"><code>application/json; charset=utf-8</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td><code>Authorization</code></td>
      <td colspan="2"><code>"Bearer "</code> + Token string obtained from<code>/auth/username</code> or <code>/auth/key</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4">Request Body</th>
    </tr>
    <tr>
      <td><code>name</code></td>
      <td>The bar's display name</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td><code>items</code></td>
      <td>The bar's items</td>
      <td><code>Array</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td>&nbsp;&nbsp;&nbsp;&nbsp;<code>[i].kind</code></td>
      <td>The bar item's type</td>
      <td><code>BarItemKind</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td>&nbsp;&nbsp;&nbsp;&nbsp;<code>[i].is_primary</code></td>
      <td>If the item should be shown on the primary bar</td>
      <td><code>Boolean</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td>&nbsp;&nbsp;&nbsp;&nbsp;<code>[i].configuration</code></td>
      <td>The bar item's configuration, depending on its <code>kind</code></td>
      <td><code>object</code></td>
      <td>Optional</td>
    </tr>
    <tr>
      <th colspan="4"><code>200</code> Response Body</th>
    </tr>
    <tr>
      <td colspan="4">Empty indicates success</td>
    </tr>
    <tr>
      <th colspan="4"><code>400</code> Response Body</th>
    </tr>
    <tr>
      <td rowspan="2"><code>error</code></td>
      <td>Missing required fields</td>
      <td colspan="2"><code>"missing_required"</code></td>
    </tr>
    <tr>
      <td>Cannot un-share the community's default bar</td>
      <td colspan="2"><code>"default_must_be_shared"</code></td>
    </tr>
    <tr>
      <td><code>details</code></td>
      <td>Specific error details</td>
      <td><code>object</code></td>
      <td>Optional</td>
    </tr>
    <tr>
      <td>&nbsp;&nbsp;&nbsp;&nbsp;<code>.required</code></td>
      <td><code>missing_required</code> list of missing field names</td>
      <td><code>string[]</code></td>
      <td>Required</td>
    </tr>
  </tbody>
</table>

### DELETE

Delete a bar configuration

<table>
  <tbody>
    <tr>
      <th colspan="4">Headers</th>
    </tr>
    <tr>
      <td><code>Authorization</code></td>
      <td colspan="2"><code>"Bearer "</code> + Token string obtained from<code>/auth/username</code> or <code>/auth/key</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4"><code>200</code> Response Body</th>
    </tr>
    <tr>
      <td colspan="4">Empty indicates success</td>
    </tr>
    <tr>
      <th colspan="4"><code>400</code> Response Body</th>
    </tr>
    <tr>
      <td rowspan="2"><code>error</code></td>
      <td>Cannot delete the communitie's default bar</td>
      <td colspan="2"><code>"cannot_delete_default"</code></td>
    </tr>
    <tr>
      <td rowspan="2"><code>error</code></td>
      <td>Cannot delete a bar that is in use</td>
      <td colspan="2"><code>"cannot_delete_used"</code></td>
    </tr>
  </tbody>
</table>

<a name="endpoint-community-skype-meetings"></a>/v1/communities/{id}/skype/meetings
------------------

### GET

Proxy for `https://api.join.skype.com/v1/meetnow/createjoinlinkguest`, which enforces CORS
restrictions on browser-based requests.

<table>
  <tbody>
    <tr>
      <th colspan="4">Headers</th>
    </tr>
    <tr>
      <td><code>Authorization</code></td>
      <td colspan="2"><code>"Bearer "</code> + Token string obtained from<code>/auth/username</code> or <code>/auth/key</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4">Request Body</th>
    </tr>
    <tr>
      <td><code>Title</code></td>
      <td>The title for the new meeting</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4"><code>200</code> Response Body</th>
    </tr>
    <tr>
      <td><code>joinLink</code></td>
      <td>The url of the new skype meeting</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
  </tbody>
</table>


<a name="endpoint-community-billing"></a>/v1/communities/{id}/billing
------------------

### GET

Get the billing information for a community

<table>
  <tbody>
    <tr>
      <th colspan="4">Headers</th>
    </tr>
    <tr>
      <td><code>Authorization</code></td>
      <td colspan="2"><code>"Bearer "</code> + Token string obtained from<code>/auth/username</code> or <code>/auth/key</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4"><code>200</code> Response Body</th>
    </tr>
    <tr>
      <td><code>plan_id</code></td>
      <td>The community plan</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td><code>trial_end</code></td>
      <td>UNIX timestamp representing the end of the trial period</td>
      <td><code>Double</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td><code>status</code></td>
      <td>Payment status</td>
      <td><code>PaymentStatus</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td><code>contact_member_id</code></td>
      <td>The ID of the member who is the billing contact</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td><code>card</code></td>
      <td>An object representing the credit card on file</td>
      <td><code>object</code></td>
      <td>Optional</td>
    </tr>
    <tr>
      <td>&nbsp;&nbsp;&nbsp;&nbsp;<code>.brand</code></td>
      <td>The brand of the card</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td>&nbsp;&nbsp;&nbsp;&nbsp;<code>.last4</code></td>
      <td>The last four digits of the card</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
  </tbody>
</table>

#### PaymentStatus: String
* `paid` - All payments have been made (or still in trial)
* `past_due` - Payments have failed, but retries have not been exhausted
* `canceled` - User canceled their account, but it won't close until the end of the billing cycle
* `closed` - Account is closed due to user request or lack of payment


### PUT

Update the billing information for a community

<table>
  <tbody>
    <tr>
      <th colspan="4">Headers</th>
    </tr>
    <tr>
      <td><code>Authorization</code></td>
      <td colspan="2"><code>"Bearer "</code> + Token string obtained from<code>/auth/username</code> or <code>/auth/key</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4">Request Body</th>
    </tr>
    <tr>
      <td><code>plan_id</code></td>
      <td>The community plan</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td><code>contact_member_id</code></td>
      <td>The ID of the member who is the billing contact</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4"><code>200</code> Response Body</th>
    </tr>
    <tr>
      <td colspan="4">Empty indicates success</td>
    </tr>
    <tr>
      <th colspan="4"><code>400</code> Response Body</th>
    </tr>
    <tr>
      <td rowspan="3"><code>error</code></td>
      <td>Invalid plan</td>
      <td colspan="2"><code>"bad_plan_id"</code></td>
    </tr>
    <tr>
      <td>Invalid contact</td>
      <td colspan="2"><code>"bad_member_id"</code></td>
    </tr>
    <tr>
      <td>The chosen plan's member limit smaller than the community's member count</td>
      <td colspan="2"><code>"plan_limit_exceeded"</code></td>
    </tr>
  </tbody>
</table>


<a name="endpoint-community-billing-card"></a>/v1/communities/{id}/billing/card
------------------

### POST

Update the card used for billing

<table>
  <tbody>
    <tr>
      <th colspan="4">Headers</th>
    </tr>
    <tr>
      <td><code>Authorization</code></td>
      <td colspan="2"><code>"Bearer "</code> + Token string obtained from<code>/auth/username</code> or <code>/auth/key</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4">Request Body</th>
    </tr>
    <tr>
      <td><code>token</code></td>
      <td>The stripe card token id</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4"><code>200</code> Response Body</th>
    </tr>
    <tr>
      <td><code>card</code></td>
      <td>An object representing the credit card on file</td>
      <td><code>object</code></td>
      <td>Optional</td>
    </tr>
    <tr>
      <td>&nbsp;&nbsp;&nbsp;&nbsp;<code>.brand</code></td>
      <td>The brand of the card</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td>&nbsp;&nbsp;&nbsp;&nbsp;<code>.last4</code></td>
      <td>The last four digits of the card</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4"><code>400</code> Response Body</th>
    </tr>
    <tr>
      <td rowspan="2"><code>error</code></td>
      <td>The card could not be validated</td>
      <td colspan="2"><code>"invalid"</code></td>
    </tr>
  </tbody>
</table>



<a name="endpoint-community-billing-cancel"></a>/v1/communities/{id}/billing/cancel
------------------

### POST

Cancel the account at the end of the billing period

<table>
  <tbody>
    <tr>
      <th colspan="4">Headers</th>
    </tr>
    <tr>
      <td><code>Authorization</code></td>
      <td colspan="2"><code>"Bearer "</code> + Token string obtained from<code>/auth/username</code> or <code>/auth/key</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4"><code>200</code> Response Body</th>
    </tr>
    <tr>
      <td><code>billing</code></td>
      <td>The updated billing record</td>
      <td><code>Billing</code></td>
      <td>Required</td>
    </tr>
  </tbody>
</table>


<a name="section-plans"></a>Plans
=================

<a name="endpoint-plans-community"></a>/v1/plans/community
------------------

### GET

Get the list of active billing plans for Morphic Community

<table>
  <tbody>
    <tr>
      <th colspan="4">Headers</th>
    </tr>
    <tr>
      <td><code>Content-Type</code></td>
      <td colspan="2"><code>application/json; charset=utf-8</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4"><code>200</code> Response Body</th>
    </tr>
    <tr>
      <td><code>plans</code></td>
      <td>The list of active community plans</td>
      <td><code>Array</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td>&nbsp;&nbsp;&nbsp;&nbsp;<code>[i].id</code></td>
      <td>The plan's unique ID</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td>&nbsp;&nbsp;&nbsp;&nbsp;<code>[i].member_limit</code></td>
      <td>The number of members allowed under the plan</td>
      <td><code>int</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td>&nbsp;&nbsp;&nbsp;&nbsp;<code>[i].months</code></td>
      <td>The number of months in the plan's billing cycle</td>
      <td><code>int</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td>&nbsp;&nbsp;&nbsp;&nbsp;<code>[i].price</code></td>
      <td>The price in the currency's smallest denomination (e.g., cent for USD)</td>
      <td><code>int</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td>&nbsp;&nbsp;&nbsp;&nbsp;<code>[i].currency</code></td>
      <td>The currency of the price</td>
      <td><code>int</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td>&nbsp;&nbsp;&nbsp;&nbsp;<code>[i].price_text</code></td>
      <td>The price, for displaying</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <td>&nbsp;&nbsp;&nbsp;&nbsp;<code>[i].monthly_price_text</code></td>
      <td>The monthly price, for displaying. Used to compare the monthly cost with other plans.</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
  </tbody>
</table>
