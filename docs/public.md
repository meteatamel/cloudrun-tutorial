# Public service

Let's deploy a container to a public Cloud Run service.

## Cloud Run Button

[Cloud Run Button](https://github.com/GoogleCloudPlatform/cloud-run-button) is a fun and easy way of running your code on Cloud Run. You can try it out here to deploy the service in [helloworld](../helloworld) folder:

[![Run on Google Cloud](https://deploy.cloud.run/button.svg)](https://deploy.cloud.run?git_url=https://github.com/meteatamel/cloudrun-tutorial.git&dir=helloworld/csharp)

Let's go through the steps involved in actually creating and deploying the 'Hello World' service. 

## 'Hello World' service

Take a look at the service we already created in [helloworld/csharp/6.0](../helloworld/csharp/6.0) folder. It's a .NET app with a `Dockerfile`.

## Build the container

In folder where `Dockerfile` resides, build the container using Cloud Build and push it to Container Registry:

```sh
PROJECT_ID=$(gcloud config get-value project)
SERVICE_NAME=hello-http-container-dotnet50

gcloud builds submit \
  --tag gcr.io/$PROJECT_ID/$SERVICE_NAME
```

## Deploy to Cloud Run

```sh
REGION=us-central1

gcloud run deploy $SERVICE_NAME \
  --image gcr.io/$PROJECT_ID/$SERVICE_NAME \
  --allow-unauthenticated \
  --platform managed \
  --region $REGION 
```

This creates a Cloud Run service and a revision for the current configuration. In the end, you get a url that you can browse to.

You can also see the service in Cloud Run console:

![Cloud Run Console](./images/cloud-run-console.png)

## Test the service

You can test the service by visiting the url mentioned during deployment and in Cloud Run console.

```sh
SERVICE_URL=$(gcloud run services describe $SERVICE_NAME --region $REGION --format 'value(status.url)')

curl $SERVICE_URL

Hello World from .NET 6.0!
```
