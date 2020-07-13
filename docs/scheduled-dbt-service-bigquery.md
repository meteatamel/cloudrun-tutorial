# Scheduled dbt service with BigQuery

[dbt](https://docs.getdbt.com/) is an open source project to build data
transformation pipelines with supported databases such as BigQuery, Postgres,
Redshift and more.

In this sample, I want to show you how to setup a scheduled Cloud Run service
that uses [dbt](https://docs.getdbt.com/) with BigQuery backend.

I'm assuming that you already have a Google Cloud project setup with BigQuery
enabled and you have `gcloud` setup to use that project.

## Setup dbt locally with BigQuery

First, let's setup dbt locally to talk to BigQuery and then we'll look into how
to run this on Cloud Run on a schedule.

Install dbt locally. Since, we'll be talking to BigQuery, we can just
install the `dbt-bigquery`:

```sh
pip3 install --user --upgrade dbt-bigquery
```

This installs `dbt` under your Python bin directory, eg.
`/Users/atamel/Library/Python/3.7/bin/dbt`. Add this to your PATH.

Create a new dbt project:

```sh
dbt init dbt_project
```

By default, dbt looks at `~/.dbt/profiles.yml` for the backend to connect to. To
configure dbt with BigQuery, you can edit this file or better, create a new
profile file in the project directory:

```sh
cd dbt_project
cat <<EOF > profiles.yml
default:
  target: dev
  outputs:
    dev:
      type: bigquery
      method: oauth
      project: your-project-id
      dataset: temp
EOF
```

This profile uses oauth for authentication to create a temporary BigQuery dataset in your project.

Run dbt with this new profile:

```sh
$ dbt run --profiles-dir .

Running with dbt=0.17.0
Found 2 models, 4 tests, 0 snapshots, 0 analyses, 147 macros, 0 operations, 0 seed files, 0 sources

11:48:31 | Concurrency: 1 threads (target='dev')
11:48:31 |
11:48:31 | 1 of 2 START table model temp.my_first_dbt_model..................... [RUN]
11:48:34 | 1 of 2 OK created table model temp.my_first_dbt_model................ [CREATE TABLE (2) in 2.65s]
11:48:34 | 2 of 2 START view model temp.my_second_dbt_model..................... [RUN]
11:48:35 | 2 of 2 OK created view model temp.my_second_dbt_model................ [CREATE VIEW in 1.01s]
11:48:35 |
11:48:35 | Finished running 1 table model, 1 view model in 4.81s.

Completed successfully

Done. PASS=2 WARN=0 ERROR=0 SKIP=0 TOTAL=2
```

You should see a temp dataset created in BigQuery.

## Run dbt with Cloud Run

Running dbt on Cloud Run has a few challenges, namely:

1. dbt is mainly a command line tool whereas Cloud Run expects HTTP requests.
   How do you call dbt command from a Cloud Run service?
2. Cloud Run runs containers. How do you run dbt in a container?
3. How do you authenticate dbt with BigQuery? OAuth works for end users but for
   services running in the cloud, it's probably not the right solution.

For #1, Cloud Run has [an
example](https://cloud.google.com/run/docs/quickstarts/build-and-deploy#shell)
on how to run a shell command from an HTTP Server deployed to Cloud Run. It involves
setting up a Go based HTTP server that simply calls a shell script upon receiving a GET
request. You can simply copy that as [invoke.go](../dbt/invoke.go). In our case, the
shell script, [script.sh](../dbt/script.sh) calls dbt with the profile folder.

For #2, dbt has some [base
images](https://hub.docker.com/r/fishtownanalytics/dbt/tags) that you can rely
on (although the documentation is pretty much non-existent). This is a sample
[Dockerfile](../dbt/Dockerfile) that works:

```dockerfile
FROM golang:1.13 as builder
WORKDIR /app
COPY invoke.go ./
RUN CGO_ENABLED=0 GOOS=linux go build -v -o server

FROM fishtownanalytics/dbt:0.17.0
USER root
WORKDIR /dbt
COPY --from=builder /app/server ./
COPY script.sh ./
COPY dbt_project ./

ENTRYPOINT "./server"
```

In this Dockerfile, we first build the HTTP server. Then, we use the dbt base
image, copy our dbt project and also the script to call that project with the
profile. Finally, we start the HTTP server to receive requests.

For #3, Cloud Run, by default, uses the Compute Engine default service account
and that should be able to make BigQuery calls. However, it's best practice to assign a
more granular permission to your Cloud Run service by assigning a
dedicated service account with more restricted IAM roles.

In this case, create a service account with `bigquery.admin` role (you probably
want to use even a finer grained role in production):

```sh
export SERVICE_ACCOUNT=dbt-sa
gcloud iam service-accounts create ${SERVICE_ACCOUNT} \
   --display-name "DBT BigQuery Service Account"
gcloud projects add-iam-policy-binding \
  $(gcloud config get-value project) \
  --member=serviceAccount:${SERVICE_ACCOUNT}@$(gcloud config get-value project).iam.gserviceaccount.com \
  --role=roles/bigquery.admin
```

Build the container:

```sh
export SERVICE_NAME=dbt-service
gcloud builds submit \
  --tag gcr.io/$(gcloud config get-value project)/${SERVICE_NAME}
```

Deploy to Cloud Run with the service account created earlier and also
`no-allow-unauthenticated` flag to make it a private service:

```sh
gcloud run deploy ${SERVICE_NAME} \
    --image gcr.io/$(gcloud config get-value project)/${SERVICE_NAME} \
    --service-account ${SERVICE_ACCOUNT}@$(gcloud config get-value project).iam.gserviceaccount.com \
    --no-allow-unauthenticated
```

## Setup Cloud Scheduler

The final step is to call the Cloud Run service on a schedule. You can do this
with Cloud Scheduler.

First, enable the Cloud Scheduler API:

```sh
gcloud services enable cloudscheduler.googleapis.com
```

Create a service account for Cloud Scheduler with `run.invoker` role:

```sh
export SERVICE_ACCOUNT=dbt-scheduler-sa
gcloud iam service-accounts create ${SERVICE_ACCOUNT} \
   --display-name "DBT Scheduler Service Account"
gcloud run services add-iam-policy-binding ${SERVICE_NAME} \
   --member=serviceAccount:${SERVICE_ACCOUNT}@$(gcloud config get-value project).iam.gserviceaccount.com \
   --role=roles/run.invoker
```

Create a Cloud Scheduler job to call the service every 5 minutes:

```sh
export SERVICE_URL="$(gcloud run services list --platform managed --filter=${SERVICE_NAME} --format='value(URL)')"
gcloud scheduler jobs create http ${SERVICE_NAME}-job --schedule "*/5 * * * *" \
   --http-method=GET \
   --uri=${SERVICE_URL} \
   --oidc-service-account-email=${SERVICE_ACCOUNT}@$(gcloud config get-value project).iam.gserviceaccount.com \
   --oidc-token-audience=${SERVICE_URL}
```

You can test that the service gets called and the temporary BigQuery dataset
gets created by manually invoking the job:

```sh
gcloud scheduler jobs run ${SERVICE_NAME}-job
```
