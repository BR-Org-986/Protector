# Protector

## Description
Web Service designed to add branch protections to the default branch of a newly created repository.

## Organization Setup
* Create GitHub Organization
* Create a personal access token
	* Access Token Scopes admin:org_hook, repo
	
## Service Setup
```These values should be updated within the appsettings.json file prior to startup\n
# GitHub Organization name created in Organization Setup\n
"Organization": "ORGANIZATION_NAME"\n
# GitHub User to use within the created Organization\n
"OrgOwner": "ORGANIZATION_OWNER"\n
# Secret provided to the Webhook to validate the Event Signature\n
"Secret": "WEBHOOK_SECRET"\n
# Personal Access Token created in Organization Setup\n
"Token": "OWNER_TOKEN"\n
# POST URL provided to the webhook to receive the repository creation payload\n
"URL": "WEBHOOK_URL"\n
# Boolean controlling if you would like the service to handle registering the webhook on startup.\n
# Set this to false if you would prefer to setup the webhook in some other way.\n
"InitWebhook":  false```

## Startup
When the application starts, if the `InitWebhook` configuration is enabled, the webhook will be registered.

## After Startup
After everything has been deployed, configured and started, when a new repository is created with a default branch, GitHub will fire off an event to this service. After confirming the request is valid, default branch protections will be applied, and an issue created outlining those protections.