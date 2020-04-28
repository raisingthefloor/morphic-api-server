import json
import time
import urllib.parse
import urllib.request
from http.client import RemoteDisconnected
from urllib.error import HTTPError


class MorphicLite(object):
    DefaultBaseUrl = "http://localhost:5002"

    class MorphicLiteError(Exception):
        pass

    def __init__(self, url=None):
        self.baseUrl = url or self.DefaultBaseUrl

    def build_request(self, method, path, data=None, headers=None):
        if headers is None:
            headers = {}
        url = self.baseUrl + urllib.request.pathname2url(path)
        req = urllib.request.Request(url, data=data, headers=headers, method=method)
        print(method + " " + req.get_full_url())
        return req

    def build_json_request(self, method, path, data_obj=None, headers=None):
        data = json.dumps(data_obj).encode('utf-8') if data_obj else None
        req = self.build_request(method, path, data, headers)
        req.add_header('Content-Type', "application/json; charset=utf-8")
        return req

    def json_request(self, method, path, data_obj=None, headers=None):
        req = self.build_json_request(method, path, data_obj, headers)
        try:
            now = time.time()
            resp = urllib.request.urlopen(req)
            later = time.time()
            print("Request took {timeDiff}sec".format(timeDiff=(later-now)))
        except HTTPError as e:
            if 400 <= e.code < 500:
                if "json" in e.headers.get("Content-Type", ""):
                    error = json.loads(e.read())
                    raise self.MorphicLiteError(error)
                else:
                    raise self.MorphicLiteError(str(e))
            else:
                raise self.MorphicLiteError(str(e))
        except RemoteDisconnected:
            raise self.MorphicLiteError("remote server disconnected unexpectedly")
        except urllib.error.URLError:
            raise self.MorphicLiteError("Could not connect to Morphic")

        data = resp.read()
        if "utf-8" in resp.headers.get("Content-Type", ""):
            data = data.decode('utf-8')
        if "json" in resp.headers.get("Content-Type", ""):
            resp_obj = json.loads(data)
        else:
            resp_obj = data
        return resp_obj


class AuthedMorphicRequest(MorphicLite):
    def __init__(self, userId, authToken):
        super().__init__()
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
            print("Warning: returned userId {} doesn't match sent {}", prefs['user_id'], self.userId)
        if prefs['id'] != self.pref_id:
            print("Warning: returned preferencesId {} doesn't match sent {}", prefs['id'], self.pref_id)

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
