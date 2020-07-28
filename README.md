A C# implementation of a simple Morphic Lite HTTP API to serve and store user preferences.

[![Build Status](https://dev.azure.com/raisingthefloor/MorphicLite/_apis/build/status/MorphicLiteServer?branchName=master)](https://dev.azure.com/raisingthefloor/MorphicLite/_build/latest?definitionId=1&branchName=master)

Sub-Documents:
* [API Docs](Documentation/API.md)
* [Metrics Doc](Metrics.md)
* [Email Doc](Morphic.Server/Email/README.md)

Development
=====

To run the services MorphicServer depends on, you can invoke a docker stack:
````
$ docker swarm init  # only needs to be called once per computer
$ docker stack deploy -c docker-compose.dev.yaml morphic-server
````

*Currently the only dependency is MongoDB, which you can run without docker if desired*

The server itself can be started from your IDE or from the command line:
````
$ dotnet run --project Morphic.Server
````

You can get started making requests by registering a new user:
````
$ curl http://localhost:5002/v1/register/username -H 'Content-Type: application/json; charset=utf-8' --data-binary '{"username": "myusername", "password": "mypassword", "email": "user1@example.com"}'
````
This gets you a response like this:
````
{
  "token":"N07vxU53lSBDGkIyKslLDo4ciFJjlepwUZ7CstcmLdLG8hHhm7fzcQhacvTT/R2E1/oMBjT+gwpDJR7NqVZDNg==",
  "user":{
    "first_name":null,
    "last_name":null,
    "preferences_id":"174db6c9-9e4e-4867-8392-cfe8ab25fe3c",
    "id":"c6e59372-5449-44a5-b426-55b7a658d252"
  }
}
````
For further API calls, use the `token` as a Bearer authorization header such as:
````
$ curl -H 'Authorization: Bearer N07vxU53lSBDGkIyKslLDo4ciFJjlepwUZ7CstcmLdLG8hHhm7fzcQhacvTT/R2E1/oMBjT+gwpDJR7NqVZDNg==' -H 'Content-Type: application/json; charset=utf-8' http://localhost:5002/v1/users/c6e59372-5449-44a5-b426-55b7a658d252
````
which gets us the response"
````
{
  "first_name":null,
  "last_name":null,
  "preferences_id":"174db6c9-9e4e-4867-8392-cfe8ab25fe3c",
  "id":"c6e59372-5449-44a5-b426-55b7a658d252"
}
````
docker-compose and morphicserver container
=====
An alternative to docker swarm is to run the containers directly with docker-compose:
````
docker-compose -f docker-compose.dev.yaml -f docker-compose.morphicserver.yml up --build
````

Slightly nicer, if you're going to be doing work on the morphic server and don't need to bring the database
down all the time:
````
docker-compose -f docker-compose.dev.yaml -f docker-compose.morphicserver.yml up -d mongo
````
followed by one or more
````
docker-compose -f docker-compose.dev.yaml -f docker-compose.morphicserver.yml up --build morphicserver
````
The same curl command as above will work.

At the end:
````
docker-compose -f docker-compose.dev.yaml -f docker-compose.morphicserver.yml down --remove-orphans
````

Note: If you previously created a swarm and want to leave it before running docker-compose commands:
````
docker swarm leave --force
````

# Logging setup and configuration

1. https://github.com/serilog/serilog/wiki/Formatting-Output#formatting-json
2. https://github.com/serilog/serilog-formatting-compact

# Deployment Environment Variables

Any of the settings can be given as environment variables by combining the level using double-underscores `__`

Example:

      "DatabaseSettings": {
        "ConnectionString": "mongodb://localhost:27017",
        "DatabaseName": "Morphic"
      }

Can be set via:

    DATABASESETTINGS__CONNECTIONSTRING=...
    DATABASESETTINGS__DATABASENAME=...


## Required environment variables:

### Sensitive (need encryption)

These are sensitive and need to be protected/encrypted in any deployment repo:

* MORPHIC_ENC_KEY_PRIMARY
  * Format: `<name>:<value>`
     * name: any descriptive name. MUST NOT CHANGE AFTERWARDS (saved with the encrypted values in the DB)
     * recommendation: use the date-string as the name: 20200601
     * Generate value: `openssl enc -aes-256-cbc -k <somepassphrase> -P -md sha1 | grep key`
* MORPHIC_HASH_SALT_PRIMARY
  * Format: `<name>:<value>`
    * name: any descriptive name. MUST NOT CHANGE AFTERWARDS (saved with the encrypted values in the DB)
    * recommendation: use the date-string as the name: 20200601
    * Generate value: `openssl rand -hex 16`
* DATABASESETTINGS__CONNECTIONSTRING
  * A MongoDB connectionstring. Example: `mongodb://mongo:27017/Morphic` (but usually longer)
  * If the connection string does not containt the database name, must also provide `DATABASESETTINGS__DATABASENAME`
* EMAILSETTINGS__SENDGRIDSETTINGS__APIKEY
  * Sendgrid API KEY. Get it from Sendgrid.
* HANGFIRESETTINGS__CONNECTIONSTRING
  * A MongoDB connectionstring. Example: `mongodb://mongo:27017/Morphic` (but usually longer)
  * Must include the database name.
* MORPHICSETTINGS__RECAPTCHA3SETTINGS__SECRET
  * The Google Recaptcha3 secret (goes with the key, which does not have to be encrypted)

### Non-sensitive

* DATABASESETTINGS__DATABASENAME
  * If the database name is not part of the ConnectionString, add it here. Example: `Morphic`
* SERILOG__MINIMUMLEVEL__DEFAULT
  * Logging level
* ASPNETCORE_ENVIRONMENT
  * ASP.NET Core environment descriptor. Usually 'development' or 'production' or similar. Not used.
  * see the various `appsettings.<name>.json` files.
* EMAILSETTINGS__TYPE
  * values: `sendgrid`, `disabled` (also "logs" but that's for development ONLY! LOGS email addresses.)
* EMAILSETTINGS__EMAILFROMADDRESS
  * used by sendgrid
* EMAILSETTINGS__EMAILFROMFULLNAME
  * used by sendgrid
* MORPHICSETTINGS__RECAPTCHA3SETTINGS__KEY
  * The Google Recaptcha3 key (does not have to be encrypted; goes with the secret which does.)
* MORPHICSETTINGS__FRONTENDSERVERURLPREFIX
  * full URL to the Morphic front-end-server.
  * example: `https://dev-ui.morphiclite-oregondesignservices.org`

#### Optional

* MORPHIC_ENC_KEY_ROLLOVER_* 
  * Rollover keys. **Must be encrypted!**
* DOTNET_DISABLE_EXTENDED_METRICS
  * Disable extended metrics, if they cause slowness in the server.
