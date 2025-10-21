# Deployments

## AWS Lightsail
Im not sure if this infra will automatically torn down by PAAS because I did not tag it

**CONTAINER IMAGE**
I did not want to pollute our PP API registry (in ECR) so used my Xero Docker hub account at https://hub.docker.com/r/carlpaton060/a2apaymentsnz

- build the image locally with `docker build -t xero-dotnet-sample-app .`
- tag the image with `docker tag xero-dotnet-sample-app:latest carlpaton060/a2apaymentsnz:latest`
- push the image with `docker push carlpaton060/a2apaymentsnz:latest`

**HOSTING**
Using the developer role in ap-southeast-2 and service Lightsaile

- create a new container service (I called mine container-service-1)
- Then create a deployment with config 
  - `image: docker.io/carlpaton060/a2apaymentsnz:latest`
  - `port 8080 HTTP` 
  - and public endpoint the name of the container (I called it a2a)

https://lightsail.aws.amazon.com/ls/webapp/ap-southeast-2/container-services/container-service-1/deployments

You can then view the app at https://container-service-1.0q6kzq9qvt9d2.ap-southeast-2.cs.amazonlightsail.com/

## Helm deploy & Charts

This helm config is not used at the moment because we need a service principle to deploy to k8s, in the spirit of hack-a-thon I pivoted 🛋️

The high level steps to make this work would be

```
# List available contexts
kubectl config get-contexts

# Set/switch to a specific context (YOU CAN ALSO JUST DO THIS WITH DOCKER DESKTOP)
kubectl config use-context <context-name>

# View current context
kubectl config current-context

# View cluster info for current context
kubectl cluster-info
```

Then to deploy

```
# 1. Set your Kubernetes context first
kubectl config use-context <your-cluster-context>

# 2. Verify you're connected to the right cluster
kubectl config current-context
kubectl get nodes

# 3. Navigate to the Helm chart directory
cd c:\dev\A2APaymentsNZ\infra

# 4. Deploy the application
helm upgrade --install a2apaymentsnz . -f values.yaml
```