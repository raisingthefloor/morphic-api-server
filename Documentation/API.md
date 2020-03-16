The Morphic Lite API

Endpoints
=========

* [User Registration](#section-user-registration)
  * [`/register/username`](#endpoint-register-username)
  * [`/register/key`](#endpoint-register-key)
* [Authentication](#section-authentication)
  * [`/auth/username`](#endpoint-auth-username)
  * [`/auth/key`](#endpoint-auth-key)
* [User Data](#section-user-data)
  * [`/users/{id}`](#endpoint-user)
  * [`/preferences/{id}`](#endpoint-preferences)


<a name="section-user-registration"></a>User Registration
=================

<a name="endpoint-register-username"></a>/register/username
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
      <th>Content-Type</th>
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
      <th><code>firstName</code></th>
      <td>The user's first name</td>
      <td><code>String</code></td>
      <td>Optional</td>
    </tr>
    <tr>
      <th><code>lastName</code></th>
      <td>The user's last name</td>
      <td><code>String</code></td>
      <td>Optional</td>
    </tr>
    <tr>
      <th colspan="4">Response Body</th>
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
  </tbody>
</table>

<a name="endpoint-register-key"></a>/register/key
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
      <th>Content-Type</th>
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
      <th><code>firstName</code></th>
      <td>The user's first name</td>
      <td><code>String</code></td>
      <td>Optional</td>
    </tr>
    <tr>
      <th><code>lastName</code></th>
      <td>The user's last name</td>
      <td><code>String</code></td>
      <td>Optional</td>
    </tr>
    <tr>
      <th colspan="4">Response Body</th>
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
  </tbody>
</table>


<a name="section-authentication"></a>Authentication
=================

<a name="endpoint-auth-username"></a>/auth/username
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
      <th>Content-Type</th>
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
      <th colspan="4">Response Body</th>
    </tr>
    <tr>
      <th><code>token</code></th>
      <td>The token string to use in the <code>X-Morphic-Auth-Token</code> header</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
  </tbody>
</table>

<a name="endpoint-auth-key"></a>/auth/key
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
      <th>Content-Type</th>
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
      <th colspan="4">Response Body</th>
    </tr>
    <tr>
      <th><code>token</code></th>
      <td>The token string to use in the <code>X-Morphic-Auth-Token</code> header</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
  </tbody>
</table>


<a name="section-user-data"></a>User Data
=================

<a name="endpoint-user"></a>/users/{id}
------------------

### GET

Get the user object for the given `id`

<table>
  <tbody>
    <tr>
      <th colspan="4">Headers</th>
    </tr>
    <tr>
      <th>X-Morphic-Auth-Token</th>
      <td colspan="2">Token string obtained from<code>/auth/username</code> or <code>/auth/key</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4">Response Body</th>
    </tr>
    <tr>
      <th><code>Id</code></th>
      <td>The user's unique ID</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th><code>PreferencesId</code></th>
      <td>The ID for the user's preferences</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th><code>FirstName</code></th>
      <td>The user's first name</td>
      <td><code>String</code></td>
      <td>Optional</td>
    </tr>
    <tr>
      <th><code>LastName</code></th>
      <td>The user's last name</td>
      <td><code>String</code></td>
      <td>Optional</td>
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
      <th>Content-Type</th>
      <td colspan="2"><code>application/json; charset=utf-8</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th>X-Morphic-Auth-Token</th>
      <td colspan="2">Token string obtained from<code>/auth/username</code> or <code>/auth/key</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4">Request Body</th>
    </tr>
    <tr>
      <th><code>FirstName</code></th>
      <td>The user's first name</td>
      <td><code>String</code></td>
      <td>Optional</td>
    </tr>
    <tr>
      <th><code>LastName</code></th>
      <td>The user's last name</td>
      <td><code>String</code></td>
      <td>Optional</td>
    </tr>
  </tbody>
</table>

<a name="endpoint-user"></a>/preferences/{id}
------------------

A preference id can be found in the `PreferencesId` property of a user object.

### GET

Get the preferences object for the given `id`.

<table>
  <tbody>
    <tr>
      <th colspan="4">Headers</th>
    </tr>
    <tr>
      <th>X-Morphic-Auth-Token</th>
      <td colspan="2">Token string obtained from<code>/auth/username</code> or <code>/auth/key</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4">Response Body</th>
    </tr>
    <tr>
      <th><code>Id</code></th>
      <td>The preferences unique ID</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th><code>UserId</code></th>
      <td>The ID for the user that owns the preferences</td>
      <td><code>String</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th><code>Default</code></th>
      <td>The dictionary of solution-specific preferences.  The keys are solution identifiers.  Each solution can have a completely arbitrary object for its preferences.</td>
      <td><code>{String: Object}</code></td>
      <td>Optional</td>
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
      <th>Content-Type</th>
      <td colspan="2"><code>application/json; charset=utf-8</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th>X-Morphic-Auth-Token</th>
      <td colspan="2">Token string obtained from<code>/auth/username</code> or <code>/auth/key</code></td>
      <td>Required</td>
    </tr>
    <tr>
      <th colspan="4">Request Body</th>
    </tr>
    <tr>
      <th><code>Default</code></th>
      <td>The dictionary of solution-specific preferences.  The keys are solution identifiers.  Each solution can have a completely arbitrary object for its preferences.</td>
      <td><code>{String: Object}</code></td>
      <td>Required</td>
    </tr>
  </tbody>
</table>