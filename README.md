# Payroc-LoadBalancer
Software-based load-balancer, operating at layer 4.

The solution is using Docker Compose to setup the servers and infrastructure

![Diagram](https://drive.google.com/uc?export=view&id=1wk-GOV__Ux9Wop19fIcrmOkEl_KGWLaB)

### Description
Load balancer
- Listens for tcp connection and accepts inbound traffic
- Queries Consul for up to date server list and health status
- Forwards traffic to the next server

Server
- On startup registers with Consul
- Accepts HTTP traffic
- Provides health status endpoint
- Has POST endpoint for external health status control

Consul
- Keeps track of all connected servers
- Pings each server health endpoint to determine status
- Provides an up to date list over HTTP API

### Running services
Load Balancer port for inbound traffic port: 5050, management: 9000

Individual servers running Kestrel on port: 8080/8081

Consul server manager dashboard port: 8500

Aspire Open Telemetry dashboard port: 18888, metrics collection: 4317

### Startup
If using Visudal Studio 2022 set docker-compose as startup project and run.

or

From the src folder in command line run
> docker compose up --build
