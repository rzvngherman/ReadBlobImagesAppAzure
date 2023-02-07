# ReadBlobImagesAppAzure

There are 2 projects inside sln file
1) 'ReadBlobImagesApp' - Azure app service - for the UI
2) azure function 'FileUploadFunction'

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