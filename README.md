# Protector

## Description
Web Service designed to add branch protections to the default branch of a newly created repository.

## Organization Setup
* Create GitHub Organization
* Create a personal access token
	* Access Token Scopes admin:org_hook, repo
	
## Service Setup
`These values should be updated within the appsettings.json file prior to startup`
`# GitHub Organization name created in Organization Setup`
`"Organization": "ORGANIZATION_NAME"`
`# GitHub User to use within the created Organization`
`"OrgOwner": "ORGANIZATION_OWNER"
`# Secret provided to the Webhook to validate the Event Signature`
`"Secret": "WEBHOOK_SECRET"`
`# Personal Access Token created in Organization Setup`
`"Token": "OWNER_TOKEN"`
`# POST URL provided to the webhook to receive the repository creation payload`
`"URL": "WEBHOOK_URL"`
`# Boolean controlling if you would like the service to handle registering the webhook on startup.`
`# Set this to false if you would prefer to setup the webhook in some other way.
`"InitWebhook":  false`

## Startup
When the application starts, if the `InitWebhook` configuration is enabled, the webhook will be registered.

## After Startup
After everything has been deployed, configured and started, when a new repository is created with a default branch, GitHub will fire off an event to this service. After confirming the request is valid, default branch protections will be applied, and an issue created outlining those protections.