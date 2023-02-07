# ReadBlobImagesAppAzure

There are 2 projects inside sln file
1) 'ReadBlobImagesApp' - Azure app service - for the UI
2) azure function 'FileUploadFunction'
3) links

# 1) App service ReadBlobImagesApp
- storage account with container
- app services with authentication 'Require authentication'
- also, with Configuration, Application settings, 'ConfigKeys__ClientId' ...
- app registrations
- front door and cdn profile

# 2) File Upload Function
- Use function App 'FileUploadFunction'
- See url
- Post data to '#functionAppUrl#/api/FileUpload'
- Form data:
a) key: 'File', type 'File'
b) key 'ContainerName', type 'Text'

# 3) Links
- Create an Azure Active Directory application and service principal that can access resources:
https://learn.microsoft.com/en-us/azure/active-directory/develop/howto-create-service-principal-portal

- try: Storage Accounts - List Keys
https://learn.microsoft.com/en-us/rest/api/storagerp/storage-accounts/list-keys?tabs=dotnet#code-try-0

- Getting Started with Azure Storage Resource Provider in .NET
https://learn.microsoft.com/en-us/samples/azure-samples/storage-dotnet-resource-provider-getting-started/storage-dotnet-resource-provider-getting-started/

- Bootstrap
https://getbootstrap.com/docs/5.0/layout/grid/
https://getbootstrap.com/docs/5.0/components/modal/
https://getbootstrap.com/docs/5.0/components/collapse/

https://stackoverflow.com/questions/6601715/how-to-declare-a-local-variable-in-razor
https://stackoverflow.com/questions/16106196/concatenating-strings-in-razor
https://www.aspsnippets.com/Articles/Create-ZIP-File-in-ASPNet-MVC.aspx
https://learn.microsoft.com/en-us/azure/active-directory/develop/scenario-web-app-sign-user-sign-in?tabs=aspnet
