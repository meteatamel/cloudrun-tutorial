# Cloud Run Tutorial
This tutorial shows different features of [Cloud Run](https://cloud.google.com/run/).

## Slides
There's a [presentation](https://speakerdeck.com/meteatamel/serverless-containers-with-cloud-run) that accompanies the tutorial.

[![Serverless with Cloud Run](./docs/images/serverless-containers-with-cloud-run.png)](https://speakerdeck.com/meteatamel/serverless-containers-with-cloud-run)

## Setup

[Cloud Run](https://cloud.google.com/run/) is a fully managed service, so there's no setup other than enabling Cloud Run and Cloud Build. [Cloud Run for Anthos](https://cloud.google.com/run/docs/quickstarts/prebuilt-deploy-gke) requires Anthos. This tutorial focuses on managed Cloud Run. 

To enable Cloud Build and Cloud Run:

```sh
gcloud services enable --project "${PROJECT_ID}" \
    cloudbuild.googleapis.com \
    run.googleapis.com
```

## Samples

* [Hello World Cloud Run](docs/helloworldcloudrun.md)

-------

This is not an official Google product.