import argparse
import json
import logging
import sys

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


DefaultBaseUrl = ""


def testrunner(args):
    logger = logging.getLogger('test_runner')
    logger.setLevel(logging.INFO if not args.debug else logging.DEBUG)
    h = logging.StreamHandler()
    h.setFormatter(logging.Formatter('%(asctime)s:%(levelname)s:%(message)s'))
    logger.addHandler(h)
    base_url = args.url

    for repeatAll in range(1, 2):
        for i in range(1, 1000):
            try:
                username = "myusername" + str(i)
                password = "mypassword" + str(i)
                email = username + "@example.com"
                try:
                    auth = Register(base_url, logger=logger).registerUser(username, password, email)
                except (Register.MorphicRegisterUserExists, Register.MorphicRegisterEmailExists):
                    auth = UserAuth(base_url, logger=logger).doAuth(username, password)
                logger.debug(json.dumps(auth, indent=4))
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

                if args.extra_tests:
                    try:
                        Register(base_url, logger=logger).get()
                    except MorphicLite.MorphicLiteError as e:
                        # this is expected!
                        logger.debug("Register GET got: {}", str(e.args))
                    else:
                        logger.error("GET Register worked but shouldn't have")

                kwargs = {
                    'url': base_url,
                    'userId': auth['user']['id'],
                    'authToken': auth['token'],
                    'pref_id': auth['user']['preferences_id'],
                    'logger': logger
                }
                pref_kls = Preferences(**kwargs)
                if args.extra_tests:
                    try:
                        pref_kls.post()
                    except MorphicLite.MorphicLiteError as e:
                        logger.debug("Register POST got an {}", str(e.args))
                    else:
                        logger.error("POST Register without body worked but shouldn't have")
                    try:
                        pref_kls.getBad()
                    except MorphicLite.MorphicLiteError as e:
                        logger.debug(e)
                    else:
                        logger.error("GET Bad Register worked but shouldn't have")

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
                    logger.error(e)
                try:
                    preferences = pref_kls.get()
                    added, removed, modified, same = dict_compare(my_prefs, preferences['default'])
                    if added or removed or modified:
                        logger.error("Added: {}".format(added))
                        logger.error("Removed: {}".format(removed))
                        logger.error("Modified: {}".format(modified))
                    else:
                        logger.debug("Same: {}".format(same))

                except MorphicLite.MorphicLiteError as e:
                    logger.error(e)
            except MorphicLite.MorphicLiteError as e:
                logger.error(e)
            except Exception as e:
                logger.error("Uncaught exception {}", e)


parser = argparse.ArgumentParser()
parser.add_argument('-d','--debug', action='store_true', help='debug')
parser.add_argument('--url', help='base url', default="http://localhost:5002")
parser.add_argument('--extra-tests', help='some extra negative tests', action='store_true')
args = parser.parse_args()
try:
    testrunner(args)
except KeyboardInterrupt:
    print()

