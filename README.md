---
services: active-directory
platforms: dotnet
author: jmprieur
level: 300
client: ASP.NET Web App
service: ASP.NET Web API
endpoint: AAD V2
---

# Calling an ASP.NET Web API from an ASP.NET Web application using Azure AD V2 endpoint

## About this sample

### Scenario

You expose a Web API and you want to protect it so that only authenticated users can access it. This sample shows how to expose an ASP.NET Web API so it can accept tokens issued by personal accounts (including outlook.com, live.com, and others) as well as work and school accounts from any company or organization that has integrated with Azure Active Directory.

The sample also demonstrates how an ASP.NET Web Application can request an access token on behalf of an authenticated user to access a protected Web APIs.

For more information on the On-behalf-of flow, please read [Azure Active Directory v2.0 and OAuth 2.0 On-Behalf-Of flow](https://docs.microsoft.com/azure/active-directory/develop/active-directory-v2-protocols-oauth-on-behalf-of).

## How to run this sample

> Pre-requisites: This sample requires Visual Studio 2015 Update 3 or Visual Studio 2017. Don’t have it? Download [Visual Studio 2017 for free](https://www.visualstudio.com/downloads/).

### Step 1: Download or clone this sample

You can clone this sample from your shell or command line:

  ```console
  git clone https://github.com/AzureADQuickStarts/AppModelv2-WebApp-WebAPI-OpenIDConnect-DotNet.git
  ```

### Step 2: Register your Web API - *TodoList-Service* in the *Application registration portal*

1. Sign in to the [Application registration portal](https://apps.dev.microsoft.com/portal/register-app) either using a personal Microsoft account (live.com or hotmail.com) or work or school account.
1. Give a name to your Application, such as `TodoList-Service`. Make sure that the *Guided Setup* option is **Unchecked**. Then press **Create**. 
1. The portal will assign your app a globally unique *Application ID*. Copy this as you'll use later in your code.
1. Click **Add Platform**, and select **Web**

> Note: When you add a *Web* the Application registration portal, it adds a pre-defined App Id URI and Scope, using the format *api://{Application Id}/{Scope Name}* named **access_as_user** (you can review it by clicking 'Edit' button). This sample code uses this default scope.

### Step 3: Configure your *TodoList-Service* project to match the Web API you just registered

1. Open the solution in Visual Studio and then open the **Web.config** file under the root of **TodoList-Service** project.
1. Replace the value of `ida:ClientId` parameter with the **Application Id** from the application you just registered in the Application Registration Portal.

#### Step 3.1: Add the new scope to the *TodoList-WebApp*`s web.config

1. Open the **web.config** file located in **TodoList-WebApp** project's root folder
1. Find the app key `ida:TodoListServiceScope` and paste the **Application Id** you copied earlier, replacing the string `{Enter the Application Id of your TodoList-Service from the app registration portal}`. 
    > Note: Make sure it uses has the format `api://{TodoList-Service-Application-Id}/access_as_user` (where {TodoList-Service-Application-Id} is the Guid representing the Application Id for your TodoList-Service).

### Step 4: Register the *TodoList-WebApp* application in the *Application registration portal*

In this step, you configure your *TodoList-WebApp* project by registering a new application in the Application registration portal. In the cases where the client and server are considered *the same application* you may also just reuse the same application registered in the 'Step 2.'.

1. Go back to [Application registration portal](https://apps.dev.microsoft.com/portal/register-app) to register a new application
1. Give a name to your Application, such as `TodoList-WebApp`, make sure that the *Guided Setup* option is **Unchecked**. Then press **Create**.
1. The portal will assign your app a globally unique *Application ID*. Copy this as you'll use later in your code.
1. Click **Add Platform**, and select **Web**.
1. In the Redirect URLs field, add `https://localhost:44326/` - which is the *TodoList-WebApp* project's SSL URL.
1. Under **Application Secrets**, click **Generate New Password**. Copy the password to a safe location as you won't be able to access this value again after you leave this dialog.

### Step 5: Configure your *TodoList-WebApp* project

1. Open the **web.config** file located in the **TodoList-WebApp** project's root folder.
1. Find the app key `ida:ClientSecret` and paste the password copied from the previous step.
1. Find the app key `ida:ClientId` and replace the existing value with the **Application Id** of the **TodoList-WebApp** that you copied in the previous step.

### Step 6: Run your project

1. Clean the solution, rebuild the solution, and run it.  You might want to go into the solution properties and set both projects as startup projects, with the service project starting first. 
1. Press `<F5>` to run your projects. Your *TodoList-WebApp* should open.
1. Select **Sign in** in the top right and sign in either by using the same a account or an account from the same directory used to register your applications.
1. At this point, if you are signing in for the first time, you may be prompted to consent to *TodoList-Service* Web Api.
1. Select **To-Do** menu to request an access token to the *access_as_user* scope on behalf of the logged user to access *TodoList-Service* Web Api and manipulate the *To-Do* list.


## Optional: Restrict sign-in to your application

By default, when you download this code sample and configure the application to use the Azure Active Directory v2 endpoint by following the steps above, both personal accounts - like outlook.com, live.com, as well as Work or school accounts can log into this application. This is typically authentication scenario for SaaS applications.

You can restrict who can sign in to your application though. To do so, use one of the following options:

### Option 1: Restrict access to a single organization (single-tenant)

You can restrict sign-in access for your application to only user accounts that are in a single Azure AD tenant - including *guest accounts* of that tenant. This scenario is common ib *line-of-business (LOB)* applications:

1. In the **web.config** file of your **TodoList-WebApp**, change the value for the `ida:Tenant` parameter from `Common` to the tenant's domain name, such as `contoso.onmicrosoft.com`.
2. Open **App_Start\Startup.Auth** file and set the `ValidateIssuer` argument to `true`.

### Option 2: Restrict access to a list of known organizations (Azure AD tenants).

You can restrict sign-in to  user accounts that are in Azure AD tenants that is in your list of allowed organizations:

1. In your **App_Start\Startup.Auth** file, set the `ValidateIssuer` argument to `true`.
2. Add the tenant ids of the organizations that you want to allow in the `ValidIssuers` parameter.

### Option 3: Restrict the types of users that can sign-in to your application

This scenario is a common for *SaaS* applications that are focused on either consumers or organizations, therefore want to block accepting either personal accounts or work or school accounts.

1. In the **web.config** file of your **TodoList-WebApp**, use on of the values below for `Tenant` parameter to control the kind of user accounts that you want your app to accept:

    Value | Description
    ----- | --------
    `common` | Users can sign in with any Work and School account, or Microsoft Personal account
    `organizations` |  Users can sign in with any Work and School account
    `consumers` |  Users can sign in with a Microsoft Personal account
     |  

    > Note: The values listed above are not considered a *tenant*, but is more of a *convention* to restrict certain categories of users.

#### Option 4: Use a custom method to validate issuers

You can implement a custom method to validate issuers by using the **IssuerValidator** parameter. For more information about how to use this parameter, read about the [TokenValidationParameters class](https://msdn.microsoft.com/library/system.identitymodel.tokens.tokenvalidationparameters.aspx) on MSDN.

