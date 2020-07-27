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
* [User Un-Registration](#section-user-unregistration)
  * [`/v1/users/{userId}/unregister`](#endpoint-user-unregister)

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

<a name="endpoint-password"></a>/v1/user/{userId}/password
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


<a name="endpoint-user-communities"></a>/v1/user/{id}/communities
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


<a name="endpoint-user-community"></a>/v1/user/{uid}/communities/{id}
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
  </tbody>
</table>

#### BarItemKind: String
* `link`
* `application`
* `action`

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
      <td rowspan="4"><code>error</code></td>
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
* `link`
* `application`
* `action`

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



<a name="section-user-unregistration"></a>User Un-Registration
=================

<a name="endpoint-user-unregister"></a>/v1/users/{userId}/unregister
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