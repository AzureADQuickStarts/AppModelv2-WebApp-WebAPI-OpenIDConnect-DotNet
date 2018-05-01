---
services: active-directory
platforms: dotnet
author: jmprieur
level: 300
client: ASP.NET Web App
service: ASP.NET Web API
endpoint: AAD V2
---

# Calling an ASP.NET Web API from an ASP.NET Web application using Azure AD V2

## About this sample

### Scenario

You expose a Web API and you want to protect it so that only authenticated user can access it. This sample shows how to expose a ASP.NET Web API so it can accept tokens issued by personal accounts (including outlook.com, live.com, and others) as well as work and school accounts from any company or organization that has integrated with Azure Active Directory. The sample also demonstrate an ASP.NET Web Application can request an access token on behalf of an user to access these protected Web APIs.

For more information about the On-behalf-of flow please see [this document](https://docs.microsoft.com/azure/active-directory/develop/active-directory-v2-protocols-oauth-on-behalf-of).

## How to run this sample

> Pre-requisites: This sample requires Visual Studio 2015 Update 3 or Visual Studio 2017. Don’t have it? Download [Visual Studio 2017 for free](https://www.visualstudio.com/downloads/).

### Step 1: Download or clone this sample

You can clone this sample from your shell or command line:

  ```console
  git clone https://github.com/AzureADQuickStarts/AppModelv2-WebApp-WebAPI-OpenIDConnect-DotNet.git
  ```

### Step 2: Register your Web API - *TodoList-Service* in the *Application registration portal*

1. Sign in to the [Application registration portal](https://apps.dev.microsoft.com/portal/register-app) either using a personal Microsoft account (live.com or hotmail.com) or work or school account.
1. Give a name to your Application, such as `WebApp-WebAPI-OpenIDConnect-DotNet-TodoList-Service`. Make sure that the *Guided Setup* option is **Unchecked**. Then press **Create**. The portal will assign your app a globally unique *Application ID* that you'll use later in your code.
1. Click **Add Platform**, and select **Web API**

> Note: When you add Web API, the *Application registration portal* already add a pre-defined App Id URI and Scope, using the format *api://{Application Id}/{Scope Name}* named **access_as_user** (you can review it by clicking 'Edit' button). This sample code uses this default scope.

### Step 3: Configure your *TodoList-Service* project to match the application you just registered

1. Open the solution in Visual Studio to configure and then open the **Web.config** file under the root of **TodoList-Service** project.
1. Replace the value of `ida:ClientId` parameter with the *Application Id* from the application Id you just registered in the Application Registration Portal.

#### Step 3.1: Add the scope created to *TodoList-Service* in *TodoList-WebApp*`s project

1. Open the **web.config** file located in **TodoList-WebApp** project's root folder and then paste **Application Id** from the application you just registered for your *TodoList-Service* under `MyWebAPIScope` parameter, replacing the string `{Enter the Application Id of your TodoList-Service from the app registration portal}`. Make sure it uses has the format `api://{Application-Id}/access_as_user`.

### Step 4: Register the *TodoList-WebApp* application in the *Application registration portal*

In this step, you configure your *TodoList-WebApp* projectby registering a new application in the Application registration portal. In the cases where the client and server are considered *the same application* you may also just reuse the same application registered in the 'Step 2.'.

1. Go back to [Application registration portal](https://apps.dev.microsoft.com/portal/register-app) to register a new application
1. Give a name to your Application, make sure that the *Guided Setup* option is **Unchecked**. Then press **Create**.
1. Click **Add Platform**, and select **Web**.
1. In the Redirect URLs field, add `https://localhost:44326/` - which is the *TodoList-WebApp* project's SSL URL
1. Under **Application Secrets**, click **Generate New Password**. Copy the password to a safe location as it won't be displayed anymore: you will need use this value in the next step.

### Step 5: Configure your *TodoList-WebApp* project

1. Open the **web.config** file located in the **TodoList-WebApp** project's root folder and then paste the password from the previous step in the `ida:ClientSecret` parameter value
1. Go back to the *Application registration portal*, copy the value of the **Application Id**, and then paste it under `ida:ClientId` parameter value

### Step 6: Run your project

1. Press `<F5>` to run your project. Your *TodoList-WebApp* should open.
1. Select **Sign in** in the top right and sign in either by using a personal Microsoft account (live.com or hotmail.com) or work or school account.
1. At this point, if you are signing in for the first time, you may be prompted to consent to *TodoList-Service* Web Api.
1. Select **To-Do** menu to request an access token to the *access_as_user* scope on behalf of the logged user to access *TodoList-Service* Web Api and manipulate the *To-Do* list.

## Optional: Restrict sign-in access to your application

By default, when download this code sample and configure the application to use the Azure Active Directory v2 endpoint following the preceeding steps, both personal accounts - like outlook.com, live.com, and others - as well as Work or school accounts from any organizations that are integrated with Azure AD can sign in to your application. This is typically used on SaaS applications.

To restrict who can sign in to your application, use one of the options:

### Restrict access to a single organization (single-tenant)

You can restrict sign-in access for your application to only user accounts that are in a single Azure AD tenant - including *guest accounts* of that tenant. This scenario is a common for *line-of-business applications*:

1. In the **web.config** file of your **TodoList-WebApp**, change the value for the `Tenant` parameter from `Common` to the tenant name of the organization, such as `contoso.onmicrosoft.com`.
2. In your [OWIN Startup class](#configure-the-authentication-pipeline), set the `ValidateIssuer` argument to `true`.

### Restrict access to a list of organizations

You can restrict sign-in access to only user accounts that are in a specific list of Azure AD organizations:

You can restrict sign-in access to only user accounts that are in an Azure AD organization that is in the list of allowed organizations:

1. In your [OWIN Startup class](#configure-the-authentication-pipeline), set the `ValidateIssuer` argument to `true`.
2. Set the value of the `ValidIssuers` parameter to the list of allowed organizations.

#### Use a custom method to validate issuers

You can implement a custom method to validate issuers by using the **IssuerValidator** parameter. For more information about how to use this parameter, read about the [TokenValidationParameters class](https://msdn.microsoft.com/library/system.identitymodel.tokens.tokenvalidationparameters.aspx) on MSDN.

