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
