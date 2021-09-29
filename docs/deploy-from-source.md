# Deploy from source

Cloud Run now supports deploying directly from source with a single CLI command `gcloud run deploy`. Source code is uploaded to Cloud Build, which uses GCP Buildpacks or an included Dockerfile to build a container.  The result is pushed to Artifact Registry and deployed to Cloud Run. You can read more about it in [deploying from source code](https://cloud.google.com/run/docs/deploying-source-code) docs.

## 'Hello World' service

Take a look at the service we already created in [helloworld/csharp/3.1](../helloworld/csharp/3.1) folder. It's a .NET Core 3.1 app and it doesn't have a `Dockerfile`.

## Deploy to Cloud Run

Inside the source folder:

```sh
SERVICE_NAME=helloworld
REGION=us-central1

gcloud run deploy $SERVICE_NAME \
  --source . \
  --allow-unauthenticated \
  --platform managed \
  --region $REGION 
```

This uploads sources to Cloud Build, uses Buildpacks to build a container and then deploy to a Cloud Run service.

## Test the service

You can test the service by visiting the url mentioned during deployment and in Cloud Run console.

```sh
SERVICE_URL=$(gcloud run services describe $SERVICE_NAME --region $REGION --format 'value(status.url)')

curl $SERVICE_URL

Hello World from .NET Core 3.1!
```
