# Build and deploy

## Pipeline architecture

The following diagram illustrates the process of this service's pipeline:

<!--
If edits are required, you can make a copy of this diagram on Miro:
https://miro.com/app/board/uXjVM-0nhl8=/
-->

![Deployment architecture diagram](https://accelerators.xero.dev/docs/api-accelerator/images/deployment-pipeline.jpg)

[View image on GitHub](https://github.com/xero-internal/accelerators-documentation/tree/master/docs/worker-accelerator/images/deployment-pipeline.jpg) | [Edit in Miro](https://miro.com/app/board/uXjVPjMaNVo=/)

1. An engineer pushes a commit to the Git repository.

2. Github Actions(GHA) detects the new change and runs:
    - **Get config values**
    - **Test and SonarScan**
    - **Run Static Analysis**
    - (main only) **Calculate package version**
    - (main only) **Work Dir changes**
    - (main only) **Terraform validate**
    - (pr only) **Check jira references**

3. If all tests passed and the change was made to the default branch (`main`), GHA runs the **Build and Push Docker Image** job. This does the following:
   1. Builds a Docker container image for your worker.
   2. Pushes the generated container image into your Artifactory repository

4. If the container image was successfully pushed to Artifactory, GHA starts the deployment for the **Uat** environment.
   - **A.** [Deploy Track](https://github.dev.xero.com/ecosystem/deploytrack) event is sent. This posts a deployment notification to your Slack deployment notifications channel and creates a New Relic release event which can be viewed from the [New Relic Change register](https://onenr.io/08wo6dXXqQx).
   - **B.** The step **Deploy Infra** applies the Terraform code defined in your worker's Git repository to create a **service execution role** and other infrastructure resources, only if there are changes to your `/infra/terraform` folder.
   - **C.** The step **Deploy Worker** creates a Kubernetes deployment in your Kubernetes namespace in the **Uat** PaaS Kubernetes cluster. PaaS Kubernetes does the following
      - **I.** Downloads the Docker image that was pushed to your Artifactory repository during the **Build and Push Docker Image** step.
      - **II.** Creates pods based on the image, and configures them to run as the **service execution role** created in the **Deploy Infra** step.
      - **III.** Reports whether the deployment succeeded back to GHA.
   - **D.** Another Deploy Track event is sent. This posts a deployment notification to your Slack deployment notifications channel and creates a New Relic release event which can be viewed from the New Relic Change register.

5.  After deploying to the **Uat** environment, an engineer can manually promote the change to **Production** environments by running the **Deploy Worker to Prod** job respectively.

6.  Tag is pushed to Git, and a GitHub Release is created in your repository based on the deployment version. The deployment version is calculated in the **Calculate package version** job using [GitVersion](https://gitversion.net/docs/). The default mode of `ContinousDeployment` will automatically create the first release at `0.1.0`, and automatically bump the patch version for subsequent releases.
