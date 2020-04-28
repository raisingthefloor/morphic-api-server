
import json

from morphiclite import MorphicLite, Register, UserAuth, Preferences


def dict_compare(d1, d2):
    d1_keys = set(d1.keys())
    d2_keys = set(d2.keys())
    intersect_keys = d1_keys.intersection(d2_keys)
    added = d1_keys - d2_keys
    removed = d2_keys - d1_keys
    modified = {o: (d1[o], d2[o]) for o in intersect_keys if d1[o] != d2[o]}
    same = set(o for o in intersect_keys if d1[o] == d2[o])
    return added, removed, modified, same


def testrunner():
    for repeatAll in range(1, 2):
        for i in range(1, 1000):
            try:
                username = "myusername" + str(i)
                password = "mypassword" + str(i)
                email = username + "@example.com"
                try:
                    auth = Register().registerUser(username, password, email)
                except (Register.MorphicRegisterUserExists, Register.MorphicRegisterEmailExists):
                    auth = UserAuth().doAuth(username, password)
                print(json.dumps(auth, indent=4))
                # {
                #     "token": "bAGfAo8f9kwt3jH7Xnwz5aO+AhX+MQ/f/rqhteZHCmKChcxhPZz3QyLJOfFz7Bx+EWfNBMqUWSJ2sxXpA4A8Zg==",
                #     "user": {
                #         "first_name": null,
                #         "last_name": null,
                #         "preferences_id": "133066a8-cc09-4214-902d-40b63e038437",
                #         "id": "1a4d7d30-0d65-4f7e-b494-2901b2406f5e"
                #     }
                # }

                #    "AuthenticatedUserPreferenceId": "133066a8-cc09-4214-902d-40b63e038437",
                #    "AuthenticatedUserUid": "1a4d7d30-0d65-4f7e-b494-2901b2406f5e",

                try:
                    Register().get()
                except MorphicLite.MorphicLiteError as e:
                    print(e)

                pref_kls = Preferences(userId=auth['user']['id'], authToken=auth['token'],
                                       pref_id=auth['user']['preferences_id'])
                try:
                    pref_kls.post()
                except MorphicLite.MorphicLiteError as e:
                    print(e)
                try:
                    pref_kls.getBad()
                except MorphicLite.MorphicLiteError as e:
                    print(e)

                my_prefs = {
                    'com.mystuff.something': {
                        'foo': 'bar',
                        'bla': 1
                    },
                    'com.mystuff.somethingelse': {
                        'morecomplex': {
                            'bla': 'foo'
                        }
                    }
                }

                try:
                    pref_kls.put(my_prefs)
                except MorphicLite.MorphicLiteError as e:
                    print(e)
                try:
                    preferences = pref_kls.get()
                    added, removed, modified, same = dict_compare(my_prefs, preferences['default'])
                    if added or removed or modified:
                        print("Added: {}".format(added))
                        print("Removed: {}".format(removed))
                        print("Modified: {}".format(modified))
                    else:
                        print("Same: {}".format(same))

                except MorphicLite.MorphicLiteError as e:
                    print(e)
            except MorphicLite.MorphicLiteError as e:
                print(e)


testrunner()
