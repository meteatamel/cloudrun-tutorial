# Service to service authentication

In this example, we'll see how to setup service-to-service auth between two Cloud Run services. More specifically: 
* We'll deploy a service that makes a call to another service and see that it fails to make the call. 
* We'll setup service-to-service auth in the wrong way to make the call work. 
* We'll give our service an identity and do the service-to-service authentication the right way. 

## Deploy a receiving service

First, let's build and deploy a *private* Cloud Run service that will receive calls. You can check out the code in [auth/receiving](../auth/receiving) but it is a service that simply echoes back with `Hello World`. 

Deploy the service with `no-allow-unauthenticated` flag:

```bash
export SERVICE_NAME=receiving

gcloud builds submit \
  --project ${PROJECT_ID} \
  --tag gcr.io/${PROJECT_ID}/receiving

gcloud run deploy ${SERVICE_NAME} \
  --image gcr.io/${PROJECT_ID}/receiving \
  --platform managed \
  --no-allow-unauthenticated
```

## Deploy a calling service

Second, let's build and deploy a Cloud Run service that calls the receiving Cloud Run service. The code is in [auth/calling](../auth/calling) folder. When it receives a request, it makes an HTTP GET request to the passed in env var URL.

Get the url of the receiving service and deploy the public service pointing to that url:

```bash
export PRIVATE_SERVICE_URL="$(gcloud run services list --platform managed --filter=receiving --format='value(URL)')"

export SERVICE_NAME=calling

gcloud builds submit \
  --project ${PROJECT_ID} \
  --tag gcr.io/${PROJECT_ID}/calling


gcloud run deploy ${SERVICE_NAME} \
  --image gcr.io/${PROJECT_ID}/calling \
  --platform managed \
  --allow-unauthenticated \
  --set-env-vars URL=${PRIVATE_SERVICE_URL}
```

## Test the calling service

Test the calling service:

```bash
export SERVICE_URL="$(gcloud run services list --platform managed --filter=calling --format='value(URL)')"

curl ${SERVICE_URL}

Second service says:
<html><head>
<meta http-equiv="content-type" content="text/html;charset=utf-8">
<title>403 Forbidden</title>
</head>
<body text=#000000 bgcolor=#ffffff>
<h1>Error: Forbidden</h1>
<h2>Your client does not have permission to get URL <code>/</code> from this server.</h2>
<h2></h2>
</body></html>
```

As expected, the calling service gets a `Forbidden` error from the receiving service because it's not authenticated. 

## Service-to-service auth overview

For service-to-service auth to work, two things must happen:

1. The receiving service must be configured to accept requests from the calling service. 
2. The calling service must identify itself to the receiving service.

We'll get back to #1 for now and focus on #2 in the next section. 

## Identify calling service to the receiving service

A Cloud Run service can use an identity token to identify itself to another Cloud Run service. But where do you get an identity token? You can use the Compute Metadata Server to fetch identity tokens with a specific audience (i.e. another Cloud Run service url) as follows:

```
curl "http://metadata.google.internal/computeMetadata/v1/instance/service-accounts/default/identity?audience=[AUDIENCE]" \
  -H "Metadata-Flavor: Google"
```

In the modified version of our calling service in [auth/authenticated](../auth/authenticated) folder, we get an id token in `GetIdToken` method and then pass that token as an authorization header. Check out [DefaultController.cs](../auth/authenticated/Controllers/DefaultController.cs) for details. 

Let's build and deploy the modified calling service. In [auth/authenticated](../auth/authenticated) folder, first get the service url of the private service and then deploy the modified calling service:

```bash
export PRIVATE_SERVICE_URL=...

export SERVICE_NAME=authenticated

gcloud builds submit \
  --project ${PROJECT_ID} \
  --tag gcr.io/${PROJECT_ID}/authenticated

gcloud run deploy ${SERVICE_NAME} \
  --image gcr.io/${PROJECT_ID}/authenticated \
  --platform managed \
  --allow-unauthenticated \
  --set-env-vars URL=${PRIVATE_SERVICE_URL}
```

## Test the calling service

Test the modified calling service:

```bash
export SERVICE_URL=...

curl ${SERVICE_URL}

Second service says: Hello World!
```

But why does it work? Even though we identified our calling service to the receiving service, we didn't configure the receiving service to accept requests from the calling service (we ignored #1). 

As explained in [service identity docs](https://cloud.google.com/run/docs/securing/service-identity), by default, Cloud Run services use the Compute Engine default service account `(PROJECT_NUMBER-compute@developer.gserviceaccount.com)`, which has the Project > Editor IAM role. This means that by default, your Cloud Run revisions have read and write access to all resources in your GCP project. 

This is why it worked but it's not ideal.

## Give calling service an identity

Let's now give the calling service an identity. First, create a Service Account:

```bash
export SERVICE_ACCOUNT=cloudrun-authenticated-sa

gcloud iam service-accounts create ${SERVICE_ACCOUNT} \
   --display-name "Cloud Run Authenticated Service Account"
```

Deploy the calling service with the new identity:

```bash
export SERVICE_NAME=authenticated

gcloud run services update ${SERVICE_NAME} \
  --service-account ${SERVICE_ACCOUNT}@${PROJECT_ID}.iam.gserviceaccount.com \
  --platform managed
```

At this point, if you try the calling service, it'll get a `Forbidden` error from the receiving service because it's not associated with the default Compute Engine service account. 


## Configure the receiving service to accept requests from the calling service

We need to configure the receiving service to accept requests from the calling service. This is done by giving the service account of the calling service the Cloud Run invoke role on the receiving service:  

```bash
export SERVICE_NAME=receiving

gcloud run services add-iam-policy-binding ${SERVICE_NAME} \
  --member=serviceAccount:${SERVICE_ACCOUNT}@${PROJECT_ID}.iam.gserviceaccount.com \
  --role=roles/run.invoker \
  --platform managed
```

Finally, everything works as expected:

```bash
export SERVICE_URL=...

curl ${SERVICE_URL}

Second service says: Hello World!
```