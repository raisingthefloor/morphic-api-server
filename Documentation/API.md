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
      <td>The token string to use in the <code>X-Morphic-Auth-Token</code> header</td>
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
      <th rowspan="4"><code>error</code></th>
      <td>Missing required fields</td>
      <td colspan="2"><code>"missing_required"</code></td>
    </tr>
    <tr>
      <td>Username already exists</td>
      <td colspan="2"><code>"existing_username"</code></td>
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
  </tbody>
</table>

<a name="endpoint-register-key"></a>/v1/register/key
------------------

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
      <td>The token string to use in the <code>X-Morphic-Auth-Token</code> header</td>
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
      <th rowspan="4"><code>error</code></th>
      <td>Missing required fields</td>
      <td colspan="2"><code>"missing_required"</code></td>
    </tr>
    <tr>
      <td>Key already exists</td>
      <td colspan="2"><code>"existing_key"</code></td>
    </tr>
  </tbody>
</table>


<a name="section-authentication"></a>Authentication
=================

<a name="endpoint-auth-username"></a>/v1/auth/username
------------------

### POST

Authenticate the given username/password credentials and return a
token that can be used in `X-Morphic-Auth-Token` headers.

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
      <td>The token string to use in the <code>X-Morphic-Auth-Token</code> header</td>
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
      <td colspan="4">Empty indicates failed authentication</td>
    </tr>
  </tbody>
</table>

<a name="endpoint-auth-key"></a>/v1/auth/key
------------------

### POST

Authenticate the given secret key credentials and return a
token that can be used in `X-Morphic-Auth-Token` headers.

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
      <td>The token string to use in the <code>X-Morphic-Auth-Token</code> header</td>
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
      <td colspan="4">Empty indicates failed authentication</td>
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
      <th><code>X-Morphic-Auth-Token</code></th>
      <td colspan="2">Token string obtained from<code>/auth/username</code> or <code>/auth/key</code></td>
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
      <th><code>X-Morphic-Auth-Token</code></th>
      <td colspan="2">Token string obtained from<code>/auth/username</code> or <code>/auth/key</code></td>
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

<a name="endpoint-user"></a>/v1/users/{uid}/preferences/{id}
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
      <th><code>X-Morphic-Auth-Token</code></th>
      <td colspan="2">Token string obtained from<code>/auth/username</code> or <code>/auth/key</code></td>
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
      <th><code>X-Morphic-Auth-Token</code></th>
      <td colspan="2">Token string obtained from<code>/auth/username</code> or <code>/auth/key</code></td>
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