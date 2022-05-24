// Copyright 2022 Google LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and

const express = require('express');
const app = express();

app.get('/', (req, res) => {
  res.send("Hello World!");
});

// This endpoint will be used in the startup probe and it artificially
// waits for 20 seconds before reporting that the container started running.
app.get('/started', (req, res) => {
    var now = Math.floor(Date.now() / 1000);
    var started = (now - startedTime) > 20;
    console.log(`/started: ${started}`);
    if (started) {
        res.status(200).send('OK: Service started');
    } else {
        res.status(503).send('Error: Service not started');
    }
});


// This endpoint will be used in the liveness probe and it simply reports
// healthy all the time.
app.get('/health', (req, res) => {
    console.log(`/health: ${healthy}`);
    if (healthy) {
        res.status(200).send('OK: Service is healthy');
    }
    else {
        res.status(503).send('Error: Service is not healthy');
    }
});

const port = parseInt(process.env.PORT) || 8080;
const startedTime = Math.floor(Date.now() / 1000);
var healthy = true;

app.listen(port, () => {
  console.log(`Listening on port ${port}`);
});