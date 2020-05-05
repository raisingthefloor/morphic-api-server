Morphic Lite Server Security

Table of Contents:

[Database Field Encryption](#database-field-encryption)

# Database Field Encryption

Database field encryption is comprised of two classes: ```public class KeyStorage``` and ```public class EncryptedField```

## KeyStorage
KeyStorage handles setting up keys (currently read from [environment variables](#environment-variables), 
but later perhaps elsewhere) and handing them out to the encryption class by name.
It supports multiple keys for [key rollover](#rollover).

One key is defined as the master key and is always given out as the `Primary` key.

All keys are named. A key is mandatory and must not change throughout the lifetime of the key
(and that can be long; see [Rollover](#rollover)). The name of the key used is stored with the 
ciphertext, so that we can pick the right key for decryption.

### Environment variables

#### Variable Names

1. ```MORPHIC_ENC_KEY_PRIMARY``` contains the primary key
2. ```MORPHIC_ENC_KEY_ROLLOVER_<something>``` can be used multiple times, as long as `<something>`
is different each time.

#### Format
The variables have a specific format: `<key-name>:<hexstring format key material>`

The key material can be generated like this:

    openssl enc -aes-256-cbc -k <somepassphrase> -P -md sha1 | grep key
    
Example:

    $ openssl enc -aes-256-cbc -k foo12345 -P -md sha1 | grep key
    key=8EF43BBD8D0ED60EFAD0CBC00BBE8DEB2792C2FC55DAB8888129E33EBB1FE4FF

##### Naming

A good name for the key is the date it was created, though it can be anything that makes
sense to people managing the system. The name will be saved with the encrypted data, so the name
of the key MUST be carried forward during rollover.

Examples:

    MORPHIC_ENC_KEY_PRIMARY="20200504:5E4FA583FDFFEEE0C89E91307A6AD56EDF2DADACDE5163C1485F3FBCC166B995"
    MORPHIC_ENC_KEY_ROLLOVER_1="20200104:E9F45B9C675409B3980256D128EC90641EADF8D0E89DB8485B65B50B35717A94"
    

## EncryptedField
EncryptedField handles the encryption and gets its keys from KeyStorage.

**Encryption** will always be done with the `Primary` key. **Decryption** must (of course) 
use the key that was used to encrypt at the time. For this, the key-name used to encrypt data
is stored with the data. This allows key rollover to happen.

### Format

Example:

    AES-256-CBC:TESTKEY:w4ZDzlTS0PgWCjvad1GHkg==:dbKuGNKKv6X2jATuUZa+mGZUAAYRuqg6vkIZkrnaLKM=

Format:

| Field             | Type           |        Comments       |
| -------------     |:------------------------:|:------------|
| Encryption-type   | String of "AES-256-CBC"  | Using AES 256 in CBC mode. Maybe more types added later                |
| Key name          | String                   |   Identifies the key used, so we can pick the right one for decryption |
| IV                | Base64 encoded string    | IV with suitable length for the `Encryption-Type`                      |
| Ciphertext        | Base64 encoded string    |                                                                        |

# Rollover

The idea is that the `Primary` key is moved to a rollover key, and a new `Primary` key is provisioned. 
Any number of rollover keys can be passed to the app (and read into KeyStorage).

The `Decrypt()` method has an `out bool isPrimary` which indicates to the caller that the decryption did or
did not use the primary key. If this is `false`, the caller is expected to 'somehow' re-encrypt the decrypted value.
This could simply be done be immediately creating a new EncryptedField object and writing this into the field
holding the encrypted value, and re-saving the object. If this is deemed to slow to do in a API-controller, then a
background thread could be used. 

Pitfalls of this scheme implementation: This strategy will only reencrypt data when it is decrypted, i.e. when it
is read. If the data is not read (because a user no longer uses the product perhaps or that Object is infrequently used)
then it can take a long time for all data to be rolled over to the new Primary key. This means that the list of
Rollver-keys could get longer and longer.

Alternative scheme: whole-sale-reencryption as a kind of migration (can run in the background)

NB: While Rollover is supported by the classes and the format of the keys and fields (mainly by allowing multiple
named keys to exist and storing the key name with the ciphertext), the actual reencrypting of the data (the actual
rolling over of the keys) is not yet implemented.
 
# Key Security

There's a few keys in play here:
1. DB-encryption-keys. I.e. `MORPHIC_ENC_KEY_PRIMARY` etc. (DEK)
2. Configuration-encryption-keys: stored in AWS KMS; used via `sops` (CEK)

In the server app, DEK's are never written to disk: They remain in memory and are read from the environment. 
Assuming the `actors` are well-defined and roles separated, only the person doing the deployment will ever see 
the keys (but not the data in the DB).

DEK's in deployment files are encrypted using `sops` which uses CEK's stored in AWS KMS to encrypt and decrypt
individual values in a attribute-value file (see secrets/all.yml in the deployment repository).

Access to the CEK's (and therefore to the DEK's) is restricted by AWS IAM.

When used inside Kubernetes, the encryption keys are passed into the environment using a kubernetes secret.
The `type: Opaque` secret is just base64 encoded (hence the name `Opaque`) and access to the secret should be 
restricted with kubernetes access controls (RBAC). In addition, data-at-rest encryption can be configured for etcd, where
the secrets (and all other kubernetes configuration) is stored. This is enabled in our deployment.

Roles:

1. DBA (or anyone with access to the DB): Since they will not be doing the deployment 
and do not have access to AWS KMS to be able to decrypt the db-encryption keys, db users 
will never be able to decrypt the data. Will only see the ciphertext.
2. Ops: Will have access to the keys (they created them, and have access to the AWS KMS/Sops keys), but
should not have access to the DB (though they may, since they provisioned that as well).
3. developers: don't need access to the production DB and are not involved with the deployment and so
should be able to neither see the encrypted data not get the decryption keys.

Of course in small companies these roles are frequently shared, so prevention of security exposures relies
on trust of the employees.

## Problem areas

1. Secrets in Kubernetes are not encrypted. Need better access controls (RBAC) for secret files or 
explore a better secret type (there's supposed to be other 3rd party mechanisms)
2. Sharing of roles, i.e. developers also doing deployment and acting as DBA's


