import argparse
import json
import logging
import random

try:
    # https://pypi.org/project/lorem/
    import lorem
    from lorem.text import TextLorem

    has_lorem = True
except ImportError:
    has_lorem = False

from morphiclite import MorphicLite, Register, UserAuth, Preferences, Users


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


def lorem_struct(recurse=0):
    d = dict()
    keys = random.randrange(2, 10)
    for i in range(0, keys):
        key = TextLorem(wsep='-', srange=(1, 2)).sentence().rstrip('.')
        # roll the dice
        options = 5 if recurse else 4
        roll = random.randrange(0, options)
        if roll == 0:  # int
            value = int(random.randrange(1, 10000))
        elif roll == 1:  # word
            value = TextLorem(srange=(1, 1)).sentence().rstrip('.')
        elif roll == 2:  # sentence
            value = TextLorem(srange=(2, 5)).sentence().rstrip('.')
        elif roll == 3:  # paragraph
            value = lorem.paragraph().rstrip('.')
        elif roll == 4:  # struct
            value = lorem_struct(recurse - 1)
        d[key] = value
    return d


def lorem_preferences():
    prefs = dict()
    nprefs = random.randrange(1, 10)
    for i in range(0, nprefs):
        name = TextLorem(wsep='.', srange=(2, random.randrange(3, 5))).sentence().rstrip('.')
        if name not in prefs:
            prefs[name] = lorem_struct(random.randrange(1, 2))
    return prefs


def hardcoded_preferences():
    return {
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


def testrunner(args):
    logger = logging.getLogger('test_runner')
    logger.setLevel(logging.INFO if not args.debug else logging.DEBUG)
    h = logging.StreamHandler()
    h.setFormatter(logging.Formatter('%(asctime)s:%(levelname)s:%(message)s'))
    logger.addHandler(h)
    base_url = args.url

    for repeatAll in range(1, args.loops + 1):
        for i in range(1, args.nusers + 1):
            try:
                username = "myusername" + str(i)
                password = "mypassword" + str(i)
                email = username + "@raisingthefloor.org"
                #email = "jan+morphictest{number}@vilhuber.com".format(number=i)
                try:
                    auth = Register(base_url, logger=logger).registerUser(username, password, email)
                except (Register.MorphicRegisterUserExists, Register.MorphicRegisterEmailExists):
                    auth = UserAuth(base_url, logger=logger).doAuth(username, password)
                logger.debug(json.dumps(auth, indent=4))

                users_kwargs = {
                    'url': base_url,
                    'userId': auth['user']['id'],
                    'authToken': auth['token'],
                    'logger': logger
                }

                try:
                    user = Users(**users_kwargs).get()
                    print(user)
                except Exception as e:
                    print(e)

                if args.extra_tests:
                    try:
                        auth = UserAuth(base_url, logger=logger).doAuth(username, password + "123")
                    except Exception:
                        # this is expected!
                        pass

                    try:
                        Users(**users_kwargs).changePassword(password, password+"123")
                        auth = UserAuth(base_url, logger=logger).doAuth(username, password+"123")
                        Users(**users_kwargs).changePassword(password+"123", password)
                    except Exception as e:
                        logger.error("Could not change password", e)

                    try:
                        Register(base_url, logger=logger).get()
                    except MorphicLite.MorphicLiteError as e:
                        # this is expected!
                        # logger.debug("Register GET got: {}", str(e.args))
                        pass
                    else:
                        logger.error("GET Register worked but shouldn't have")

                users_kwargs = {
                    'url': base_url,
                    'userId': auth['user']['id'],
                    'authToken': auth['token'],
                    'pref_id': auth['user']['preferences_id'],
                    'logger': logger
                }
                pref_kls = Preferences(**users_kwargs)
                if args.extra_tests:
                    try:
                        pref_kls.post()
                    except MorphicLite.MorphicLiteError as e:
                        # logger.debug("Register POST got an {message}".format(message=str(e.args)))
                        pass
                    else:
                        logger.error("POST Register without body worked but shouldn't have")
                    try:
                        pref_kls.getBad()
                    except MorphicLite.MorphicLiteError as e:
                        # logger.debug(e)
                        pass
                    else:
                        logger.error("GET Bad Register worked but shouldn't have")

                my_prefs = lorem_preferences() if has_lorem else hardcoded_preferences()
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
                logger.error("Uncaught exception {message}".format(message=str(e)))


parser = argparse.ArgumentParser()
parser.add_argument('-d', '--debug', action='store_true', help='debug')
parser.add_argument('--url', help='base url', default="http://localhost:5002")
parser.add_argument('--extra-tests', help='some extra negative tests', action='store_true')
parser.add_argument('-n', '--nusers', help='number of users', default=1000, type=int)
parser.add_argument('-l', '--loops', help='number of loops', default=10, type=int)

args = parser.parse_args()
try:
    testrunner(args)
except KeyboardInterrupt:
    print()
