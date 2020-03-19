A C# implementation of a simple Morphic Lite HTTP API to serve and store user preferences.

See the [API Docs](Documentation/API.md)

Development
=====

To run the services MorphicServer depends on, you can invoke a docker stack:
````
$ mkdir Database
$ docker stack deploy -c docker-compose.dev.yaml morphic-server
````

*Currently the only dependency is MongoDB, which you can run without docker if desired*

The server itself can be started from your IDE or from the command line:
````
$ dotnet run --project MorphicServer
````

You can get started making requests by registering a new user:
````
$ curl http://localhost:5002/register/username -H 'Content-Type: application/json; charset=utf-8' --data-binary '{"username": "myusername", "password": "mypassword"}'
````