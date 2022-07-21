# TwitchLiveNotifications

TwitchLiveNotifications is an Azure Function that registers a Webhook to Twitch's API to receive notifications when a stream is live. Whenever a streamer that are subscribed to starts streaming, a message will be posted to Discord and Twitter.

## Prerequisites

1. dotnet v6.0 (Download from https://dotnet.microsoft.com/en-us/download)
2. PowerShell modules. The deployment scripts depends on the four modules 
Az.Accounts, Az.KeyVault, Az.Resources, and Az.Storage. To install the exact versions I've tested with, run the following code:
      ```powershell
      @(
         @{
            Name = 'Az.Accounts'
            RequiredVersion = '2.8.0'
         }
         @{
            Name = 'Az.KeyVault'
            RequiredVersion = '4.3.0'
         }
         @{
            Name = 'Az.Resources'
            RequiredVersion = '6.0.0'
         }
         @{
            Name = 'Az.Storage'
            RequiredVersion = '4.3.0'
         }
      ) | Foreach-Object {Install-Module @_  -Scope CurrentUser -Force}
      ```
3. The project has a dependency on a fork of [Twitch.Net](https://github.com/iXyles/Twitch.Net). It needs to be cloned to the dependencies folder.  
   Make sure you are in the root of the project when running the following code:
   ```powershell
   New-Item -Path "dependencies" -ItemType Directory
   Push-Location -Path "dependencies"
   git clone 'https://github.com/SimonWahlin/Twitch.Net.git'
   Set-Location -Path 'Twitch.Net'
   git checkout 8676fc7386d5a79f4158d0abbc169926ef0976f8
   Pop-Location
   ```
## Configuration

The deployment requires a set of configuration values. Copy the file `deploy/functionapp.config.example.json` to `deploy/functionapp.config.json` and fill in the values marked as `<yourvalue>`.  

> **Note** The solution uses the Twitter V1 API to post tweets, your Twitter developer account needs to have access to the V1 API and your tokens need to be set up with write permissions.

Settings in `deploy/functionApp.parameters.json` can be modified to further customize the deployment. Especially the settings `DiscordOnStreamOnlineTemplate` and `TwitterOnStreamOnlineTemplate` can be changed to customize the messages posted.  

## Deploy solution

Run the script `deploy/fullDeploy.ps1` to deploy the whole solution.  

For a more customized experience or to re-deploy part of the solution, see section "Setup instructions running individual scripts" below.

## Setup instructions running individual scripts

If you want to setup the solution step by step instead of deploying the full solution, follow these steps:

1. Run PowerShell script `scripts/1-deployKeyVault.ps1` to deploy keyvault for configuration secrets.  
   I find it handy to keep all secrets I have to manage in a separate keyvault in a separate resource group, this way I can lifecycle manage it separately from the function app.
2. Add any secrets you want to store in the keyvault. The script `scripts/2-createSecrets.ps1` will create the recommended secrets for you.
3. Deploy the function app infrastructure. First make sure `scripts/functionApp.parameters.json` is updated with your settings, then run `scripts/deployFunctionApp.ps1`.  
   > ! Note that `<KeyVaultName>` needs to be replaced with the name of your keyvault.  
   >   `<systemName>` needs to be replaced with the name your want for your function app.  
   >   `<yourPrincipalId>` needs to be replaced with the principal id of the user that should have access to deploy the code.  
   >   Get your own principal id by running `Get-AzADUser -UserPrincipalName (Get-AzContext).Account.Id | Select-Object -ExpandProperty Id`  
   Remember to take note of the output `StorageAccountName` and `FunctionAppId`, they are needed for deploying the code.
4. Deploy the function app code. Run `scripts/4-deployCode.ps1`.
5. Replace entries in `config/subscriptions.json` with your own details and run `config/AddSubscriptions.ps1` to register your subscriptions.

## Register subscriptions

To register subscriptions, replace the configuration in `config/subscriptions.json` with your own details and run `config/AddSubscriptions.ps1`.