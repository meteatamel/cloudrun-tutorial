# Public service

Let's deploy a container to a public Cloud Run service.

## Cloud Run Button

[Cloud Run Button](https://github.com/GoogleCloudPlatform/cloud-run-button) is a fun and easy way of running your code on Cloud Run. You can try it out here to deploy the service in [helloworld](../helloworld) folder:

[![Run on Google Cloud](https://deploy.cloud.run/button.svg)](https://deploy.cloud.run?git_url=https://github.com/meteatamel/cloudrun-tutorial.git&dir=helloworld/csharp)

Let's go through the steps involved in actually creating and deploying the 'Hello World' service. 

## Create a 'Hello World' service

Cloud Run already has a [build-and-deploy](https://cloud.google.com/run/docs/quickstarts/build-and-deploy) page that shows how to deploy a `Hello World` service in various languages. 

You can either create your 'Hello World' service as described in Cloud Run docs or take a look at the service we already created in [helloworld](../helloworld) folder.

## Build the container

In folder where `Dockerfile` resides, build the container using Cloud Build and push it to Container Registry:

```bash
gcloud builds submit \
  --project ${PROJECT_ID} \
  --tag gcr.io/${PROJECT_ID}/helloworld
```

## Deploy to Cloud Run

```bash
export SERVICE_NAME=helloworld-public

gcloud run deploy ${SERVICE_NAME} \
  --image gcr.io/${PROJECT_ID}/helloworld \
  --platform managed \
  --allow-unauthenticated
```

This creates a Cloud Run service and a revision for the current configuration. In the end, you get a url that you can browse to.

You can also see the service in Cloud Run console:

![Cloud Run Console](./images/cloud-run-console.png)

## Test the service

You can test the service by visiting the url mentioned during deployment and in Cloud Run console. 

```bash
export SERVICE_URL="$(gcloud run services list --platform managed --filter=${SERVICE_NAME} --format='value(URL)')"

curl ${SERVICE_URL}

Hello World!
```
