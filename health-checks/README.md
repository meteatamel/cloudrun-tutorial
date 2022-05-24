# Cloud Run Healthchecks

> **Note:** Cloud Run Healthchecks is feature in *preview*.
> Only allow-listed projects can currently take advantage of it. Please fill the
> following [form](https://docs.google.com/forms/d/e/1FAIpQLScWCZiOrwGuEUYJXSvP_-ostVUreKt_Pq_8K53DwStr7q_w8g/viewform)
> to get your project allow-listed before attempting this sample.

In this sample, you will see how to use startup and liveness probes for fully
managed Cloud Run.

## Startup and liveness probes

You can configure startup probes to know when a container has started and is
ready to start accepting the traffic. If such a probe is configured, it disables
liveness checks until it succeeds, making sure those probes don't interfere with
the application startup. This can be used to adopt liveness checks on slow
starting containers, avoiding them getting killed by the Cloud Run before they
are up and running.

You can also configure liveness probes to know when to restart a container. For
example, liveness probes could catch a deadlock, where an application is
running, but unable to make progress. Restarting a container in such a state can
help to make the application more available despite bugs.

## Deploy a Cloud Run service

To showcase the startup and liveness probe, you will deploy a sample Node.js
application to Cloud Run. You can check the source code in [index.js](index.js).
It has two endpoints:

* `/started` endpoint will be used in the startup probe and it artificially
  waits for 20 seconds before reporting that the container started running.
* `/health` endpoint will be used in the liveness probe and it simply reports
  healthy all the time.

Deploy the service to Cloud Run in a preferred region and with
allow-unauthenticated flag enabled:

```sh
gcloud run deploy
```

## Configure the service definition file

Once the service is deployed, download its service definition file:

```sh
gcloud run services describe health-checks --format export > service.yaml
```

Add the `launch-stage` annotation to `service.yaml`. This will enable you to
deploy Cloud Run service with the alpha features:

```yaml
kind: Service
metadata:
  annotations:
    ...
    run.googleapis.com/launch-stage: ALPHA
```

Also, change the revision name from `00001` to `00002`. This will allow you to
deploy the service with a new revision name later:

```yaml
spec:
  template:
    metadata:
      annotations:
        ...
      name: health-checks-00002-siq
```

## Configure startup and liveness probes

Let's add a startup probe to the `/started` endpoint and liveness probe to the
`/health` endpoint by editing the `service.yaml` file:

```yaml
      containers:
      - image: ...
        startupProbe:
          httpGet:
            path: /started
          failureThreshold: 30
          periodSeconds: 10
        livenessProbe:
          httpGet:
            path: /health
          failureThreshold: 30
          periodSeconds: 10
```

Now you can update the service:

```sh
gcloud run services replace service.yaml
```

## Test

As the service is deploying, you can check that the logs of the Cloud Run
service to see that probes are working:

```sh
Default
2022-05-24 17:00:37.899 BST Listening on port 8080
2022-05-24 17:00:45.234 BST /started: false
2022-05-24 17:00:55.242 BST /started: false
2022-05-24 17:01:05.245 BST /started: true
2022-05-24 17:01:05.250 BST /health: true
2022-05-24 17:01:15.252 BST/health: true
2022-05-24 17:01:25.255 BST/health: true
2022-05-24 17:01:35.259 BST/health: true
```

The startup probe waits for the `/started` endpoint to report
true before it concludes that the container is running.

The liveness probe pings the `/health` endpoint every 10 seconds to see that the
container is alive.
