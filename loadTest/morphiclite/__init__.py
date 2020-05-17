import json
import os
import time
import urllib.parse
import urllib.request
from http.client import RemoteDisconnected
from urllib.error import HTTPError


class MorphicLite(object):
    class MorphicLiteError(Exception):
        pass
    class MorphicLiteRequestError(Exception):
        pass


    def __init__(self, url, logger):
        self.logger = logger
        self.baseUrl = url.rstrip(os.sep)

    def build_request(self, method, path, data=None, headers=None):
        if headers is None:
            headers = {}
        url = self.baseUrl + urllib.request.pathname2url(path)
        req = urllib.request.Request(url, data=data, headers=headers, method=method)
        return req

    def build_json_request(self, method, path, data_obj=None, headers=None):
        data = json.dumps(data_obj).encode('utf-8') if data_obj else None
        req = self.build_request(method, path, data, headers)
        req.add_header('Content-Type', "application/json; charset=utf-8")
        return req

    def json_request(self, method, path, data_obj=None, headers=None):
        now = time.time()
        req = self.build_json_request(method, path, data_obj, headers)
        response_code = None
        try:
            resp = urllib.request.urlopen(req)
            response_code = resp.code
        except HTTPError as e:
            response_code = e.code
            if 400 <= e.code < 500:
                if "json" in e.headers.get("Content-Type", ""):
                    error = json.loads(e.read())
                    raise self.MorphicLiteError(error)
                else:
                    raise self.MorphicLiteRequestError(str(e))
            else:
                raise self.MorphicLiteRequestError(str(e))
        except RemoteDisconnected:
            raise self.MorphicLiteRequestError("remote server disconnected unexpectedly")
        except urllib.error.URLError:
            raise self.MorphicLiteRequestError("Could not connect to Morphic")
        finally:
            later = time.time()
            self.logger.info("{timeDiff}:{response_code}:{method}:{url}".format(
                method=method,
                url=req.get_full_url(),
                response_code=response_code,
                timeDiff=(later - now)))

        data = resp.read()
        if "utf-8" in resp.headers.get("Content-Type", ""):
            data = data.decode('utf-8')
        if "json" in resp.headers.get("Content-Type", ""):
            resp_obj = json.loads(data)
        else:
            resp_obj = data
        return resp_obj


class AuthedMorphicRequest(MorphicLite):
    def __init__(self, userId, authToken, *args, **argv):
        super().__init__(*args, **argv)
        self.userId = userId
        self.authToken = authToken

    def build_request(self, method, path, data=None, headers=None):
        req = super().build_request(method, path, data, headers)
        req.add_header("Authorization", "Bearer %s" % self.authToken)
        return req


class Register(MorphicLite):
    DefaultRegisterUrl = "/v1/register/username"

    class MorphicRegisterUserExists(MorphicLite.MorphicLiteError):
        pass

    class MorphicRegisterEmailExists(MorphicLite.MorphicLiteError):
        pass

    def registerUser(self, username, password, email, first_name=None, last_name=None):
        path = self.DefaultRegisterUrl
        data = {'username': username,
                'password': password,
                'email': email,
                'first_name': first_name,
                'last_name': last_name
                }
        try:
            register_response = self.json_request('POST', path, data)
            return register_response
        except MorphicLite.MorphicLiteError as e:
            error = e.args[0]['error']
            if error == 'existing_username':
                raise self.MorphicRegisterUserExists(username)
            elif error == 'existing_email':
                raise self.MorphicRegisterEmailExists(email)
            else:
                raise

    # Doesn't exist. Just for testing
    def get(self):
        path = self.DefaultRegisterUrl
        register_response = self.json_request('GET', path)
        return register_response


class Unregister(MorphicLite):
    DefaultUnregisterUrl = "/v1/unregister/username"

    class MorphicUnregisterUserDoesNotExist(MorphicLite.MorphicLiteError):
        pass

    def unregisterUser(self, username, password):
        path = self.DefaultUnregisterUrl
        data = {'username': username,
                'password': password
                }
        try:
            unregister_response = self.json_request('POST', path, data)
            return unregister_response
        except MorphicLite.MorphicLiteError as e:
            error = e.args[0]['error']
            if error == 'username_not_found':
                raise self.MorphicUnregisterUserDoesNotExist(username)
            else:
                raise

class UserAuth(MorphicLite):
    DefaultAuthUrl = "/v1/auth/username"

    def doAuth(self, username, password):
        path = self.DefaultAuthUrl
        data = {'username': username,
                'password': password}
        auth = self.json_request('POST', path, data)
        return auth

    # Doesn't exist. Just for testing
    def get(self):
        path = self.DefaultAuthUrl
        auth = self.json_request('GET', path)
        return auth


class Preferences(AuthedMorphicRequest):
    DefaultPreferencesUrl = "/v1/users/{userId}/preferences/{preferencesId}"

    def __init__(self, pref_id=None, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self.pref_id = pref_id

    class MorphicPreferencesValidationError(Exception):
        pass

    def get(self):
        path = self.DefaultPreferencesUrl.format(userId=self.userId, preferencesId=self.pref_id)
        prefs = self.json_request('GET', path)
        if prefs['user_id'] != self.userId:
            self.logger.error("returned userId {} doesn't match sent {}", prefs['user_id'], self.userId)
        if prefs['id'] != self.pref_id:
            self.logger.error("returned preferencesId {} doesn't match sent {}", prefs['id'], self.pref_id)

        return prefs

    def getBad(self):
        path = self.DefaultPreferencesUrl.format(userId=self.userId, preferencesId=self.pref_id) + "1234"
        prefs = self.json_request('GET', path)
        return prefs

    # Doesn't exist. Just for testing
    def post(self):
        path = self.DefaultPreferencesUrl.format(userId=self.userId, preferencesId=self.pref_id)
        prefs = self.json_request('POST', path, {'foo': 'bar'})
        return prefs

    def put(self, my_prefs):
        for key, value in my_prefs.items():
            if not isinstance(key, str):
                raise self.MorphicPreferencesValidationError("Key {} is not string".format(key))
            if not isinstance(value, dict):
                raise self.MorphicPreferencesValidationError("Key {} does not have a dict value".format(key))

        path = self.DefaultPreferencesUrl.format(userId=self.userId, preferencesId=self.pref_id)
        prefs = self.json_request('PUT', path, {"default": my_prefs})
        return prefs


class Users(AuthedMorphicRequest):
    DefaultUsersUrl = "/v1/users/{userId}"

    def get(self):
        path = self.DefaultUsersUrl.format(userId=self.userId)
        user = self.json_request('GET', path)
        if user['user_id'] != self.userId:
            self.logger.error("returned userId {} doesn't match sent {}", user['user_id'], self.userId)

        return user

    def changePassword(self, old_password, new_password):
        path = self.DefaultUsersUrl.format(userId=self.userId) + "/changePassword"
        changeRequest = {
            'existing_password': old_password,
            'new_password': new_password
        }
        self.json_request('POST', path, data_obj=changeRequest)
